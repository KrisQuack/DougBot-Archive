using Discord;
using Discord.WebSocket;
using Microsoft.Azure.CognitiveServices.ContentModerator;
using Microsoft.Azure.CognitiveServices.ContentModerator.Models;
using System.Text;
using System.Collections.Concurrent;
using DougBot.Models;
using DougBot.Scheduler;
using System.Net.Mail;

namespace DougBot.Systems.EventBased;

public static class ContentModeration
{
    private static DiscordSocketClient _client;
    private static BlockingCollection<SocketMessage> _messagesToModerate = new BlockingCollection<SocketMessage>();

    private static readonly ContentModeratorClient _moderatorClient = new ContentModeratorClient(new ApiKeyServiceClientCredentials(Environment.GetEnvironmentVariable("CONTENT_MODERATION_TOKEN")))
    {
        Endpoint = Environment.GetEnvironmentVariable("CONTENT_MODERATION_URL")
    };

    public static async Task Monitor()
    {
        _client = Program.Client;
        _client.MessageReceived += MessageReceivedHandler;
        _ = ProcessMessages();
        Console.WriteLine("Content Moderation Initialized");
    }

    private static Task MessageReceivedHandler(SocketMessage message)
    {
        //Ignore bots
        if (message.Author.IsBot) return Task.CompletedTask;
        //Process
        _messagesToModerate.Add(message);
        return Task.CompletedTask;
    }

    private static async Task ProcessMessages()
    {
        _ = Task.Run(async () =>
        {
            foreach (var message in _messagesToModerate.GetConsumingEnumerable())
            {
                try
                {
                    string reason = "No";
                    bool isTextSafe = await CheckTextContent(message.Content);
                    bool areImagesSafe = true;
                    foreach (var attachment in message.Attachments)
                    {
                        var (isImageSafe, response) = await CheckImageContent(attachment.Url);
                        if (!isImageSafe)
                        {
                            reason = response;
                            areImagesSafe = false;
                            break;
                        }
                    }

                    if (!isTextSafe)
                    {
                        //Get messages for context and check again
                        var messageContext = await message.Channel.GetMessagesAsync(message, Direction.Before, 3).FlattenAsync();
                        //Add the message that triggered the moderation
                        messageContext = messageContext.Append(message);
                        //order the messages by date
                        messageContext = messageContext.OrderBy(x => x.CreatedAt);
                        reason = await CheckTextContext(message.CleanContent, messageContext);
                        if (reason == "No")
                        {
                            isTextSafe = true;
                        }
                    }

                    if (!isTextSafe || !areImagesSafe)
                    {
                        // Create the main embed
                        var embed = new EmbedBuilder();
                        embed.Title = "Content Moderation";
                        embed.Url = message.GetJumpUrl();
                        embed.Author = new EmbedAuthorBuilder
                        {
                            Name = $"{message.Author.GlobalName} ({message.Author.Id})",
                            IconUrl = message.Author.GetAvatarUrl()
                        };
                        embed.AddField("Author", message.Author.Mention, inline: true);
                        embed.AddField("Channel", ((ITextChannel)message.Channel).Mention, inline: true); // Changed line
                        embed.AddField("Content", string.IsNullOrEmpty(message.CleanContent) ? "Media" : message.CleanContent);
                        embed.AddField("Reason", reason);
                        //Create a list of attachments
                        var attachments = new List<string>();
                        if (!areImagesSafe)
                        {
                            foreach (var attachment in message.Attachments)
                            {
                                attachments.Add(attachment.Url);
                            }
                        }
                        // Send the embed to the log channel.
                        await SendMessageJob.Queue("567141138021089308", "886548334154760242", new List<EmbedBuilder> { embed }, DateTime.UtcNow, attachments: attachments);
                    }
                }
                catch (Exception e)
                {
                    if (e.Message.Contains("BadRequest")) continue;
                    Console.WriteLine($"[General/Warning] {DateTime.UtcNow:HH:mm:ss} ContentModerator {e}");
                }
            }
        });
    }

    private static async Task<bool> CheckTextContent(string text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return true;
        }
        await Task.Delay(1000);
        var screenResult = await _moderatorClient.TextModeration.ScreenTextAsync("text/plain", new MemoryStream(Encoding.UTF8.GetBytes(text)), language: "eng", classify: true, pII: true);
        bool isClassificationSafe = screenResult.Classification.Category1.Score < 0.95 &&
                                screenResult.Classification.Category2.Score < 0.95 &&
                                screenResult.Classification.Category3.Score < 0.95;
        // If the text contains any terms from the moderation list, the result will not be null.
        return isClassificationSafe;
    }

    private static async Task<string> CheckTextContext(string message, IEnumerable<IMessage> messageContext)
    {
        var result = await OpenAIGPT.Wah354k(
            @"
You are an AI assistant for a discord server, analyzing chat and determining if it violates any rules. You will be provided a message and its context. If a rule is broken, respond with the violation. If no rule is broken, respond 'No'.
Rules: Follow Discord's TOS, No Offensive Speech, Be Kind, No Spam, English Only, No Impersonation, No Political/Sexual/Distressing Topics.
Example 1:
Message:I fucking love olive garden
Context:{<@1235>:Hey guys in going to live garden today<@1234>:I fucking love olive garden<@1235>:haha me too thats great}
Assistant: No
Example 2:
Message:Fuck you, you dont know me
Context:{<@1235>:Hey how is everyone today<@1234>:I am good<@1236>:Fuck you, you dont know me}
Assistant: Rude behaviour
"
            ,
            $"Message:{message}\nContext:{{\n{string.Join("", messageContext.Select(m => $"{m.Author.Mention}:{m.CleanContent}"))}}}"
            );
        return result;
    }

    private static async Task<(bool, string)> CheckImageContent(string imageUrl)
    {
        var bodyModel = new BodyModel("URL", imageUrl);
        await Task.Delay(1000);
        var evaluationResult = await _moderatorClient.ImageModeration.EvaluateUrlInputAsync("application/json", bodyModel);

        string reason = null;
        bool isImageSafe = true;

        if (evaluationResult.AdultClassificationScore.HasValue && evaluationResult.AdultClassificationScore.Value > 0.5)
        {
            isImageSafe = false;
            reason = "Adult content detected";
        }
        else if (evaluationResult.RacyClassificationScore.HasValue && evaluationResult.RacyClassificationScore.Value > 0.5)
        {
            isImageSafe = false;
            reason = "Racy content detected";
        }

        if (isImageSafe)
        {
            // Perform OCR on the image
            await Task.Delay(1000);
            var ocrResult = await _moderatorClient.ImageModeration.OCRUrlInputAsync("eng", "application/json", bodyModel);
            if (ocrResult.Text != null)
            {
                // Check the text found by OCR
                bool isTextSafe = await CheckTextContent(ocrResult.Text);
                if (!isTextSafe)
                {
                    isImageSafe = false;
                    reason = "Offensive text detected in image";
                }
            }
        }

        return (isImageSafe, reason);
    }
}