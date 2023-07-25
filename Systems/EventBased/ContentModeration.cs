using Discord;
using Discord.WebSocket;
using Microsoft.Azure.CognitiveServices.ContentModerator;
using Microsoft.Azure.CognitiveServices.ContentModerator.Models;
using System.Text;
using System.Collections.Concurrent;
using DougBot.Models;

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
        if(message.Author.IsBot) return Task.CompletedTask;
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
                    bool isTextSafe = await CheckTextContent(message.Content);
                    bool areImagesSafe = true;
                    foreach (var attachment in message.Attachments)
                    {
                        bool isImageSafe = await CheckImageContent(attachment.Url);
                        if (!isImageSafe)
                        {
                            areImagesSafe = false;
                            break;
                        }
                    }
                    var response = "No";
                    if (!isTextSafe)
                    {
                        response = await CheckTextContext(message.CleanContent);
                        if(response == "No")
                        {
                            isTextSafe = true;
                        }
                    }

                    if (!isTextSafe || !areImagesSafe)
                    {
                        // Create an embed builder.
                        var embedBuilder = new EmbedBuilder();
                        // Add fields to the embed for the author, content, and link to the message.
                        embedBuilder.Title = "Content Moderation";
                        embedBuilder.Url = message.GetJumpUrl();
                        embedBuilder.Author = new EmbedAuthorBuilder
                        {
                            Name = $"{message.Author.GlobalName} ({message.Author.Id})",
                            IconUrl = message.Author.GetAvatarUrl()
                        };
                        embedBuilder.AddField("Author", message.Author.Mention, inline: true);
                        embedBuilder.AddField("Content", string.IsNullOrEmpty(message.CleanContent) ? "Media" : message.CleanContent, inline: true);
                        embedBuilder.AddField("Reason", response, inline: true);
                        if (!areImagesSafe)
                        {
                            foreach (var attachment in message.Attachments)
                            {
                                embedBuilder.ImageUrl = attachment.Url;
                            }
                        }
                        // Send the embed to the log channel.
                        var logChannel = await _client.GetChannelAsync(886548334154760242) as ITextChannel;
                        await logChannel.SendMessageAsync(embed: embedBuilder.Build());
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
        var screenResult = await _moderatorClient.TextModeration.ScreenTextAsync("text/plain", new MemoryStream(Encoding.UTF8.GetBytes(text)), language: "eng",classify: true, pII: true);
        bool isClassificationSafe = screenResult.Classification.Category1.Score < 0.95 &&
                                screenResult.Classification.Category2.Score < 0.95 &&
                                screenResult.Classification.Category3.Score < 0.95;
        // If the text contains any terms from the moderation list, the result will not be null.
        return isClassificationSafe;
    }

    private static async Task<string> CheckTextContext(string message)
    {
        var result = await OpenAIGPT.Wah354k(
            @"
You are an AI assistant for a discord server, you will analyse the chat provided and determine if it violates any of the servers rules.
If a rules is broken you will respond with who broke it and the reason
If no rule is broken you will respond only 'No'

Rules:
- Follow Discord's Terms of Service
- No Offensive Speech
- Be Kind
- No Spam or Irrelevant Posting
- English Only
- No Alternate Accounts or Impersonation
- No Political Discussions:
- No Sexual Topics
- No Extremely Distressing Topics

Example:
User: I fucking love olive garden
Assistant: No

User: Fuck you, you dont know me
Assistant: Agressive language

User: British food tastes like carboard
Assistant: Hate speach towards British people

User: This chicken tastest like ass
Assistant: No
"
            , message);
        return result;
    }

    private static async Task<bool> CheckImageContent(string imageUrl)
    {
        var bodyModel = new BodyModel("URL", imageUrl);
        await Task.Delay(1000);
        var evaluationResult = await _moderatorClient.ImageModeration.EvaluateUrlInputAsync("application/json", bodyModel);

        // If the image is considered adult or racy, the result will be true.
        bool isImageSafe = !(
            (evaluationResult.AdultClassificationScore.HasValue && evaluationResult.AdultClassificationScore.Value > 0.5) ||
            (evaluationResult.RacyClassificationScore.HasValue && evaluationResult.RacyClassificationScore.Value > 0.5)
            );

        if (isImageSafe)
        {
            // Perform OCR on the image
            await Task.Delay(1000);
            var ocrResult = await _moderatorClient.ImageModeration.OCRUrlInputAsync("eng", "application/json", bodyModel);
            if (ocrResult.Text != null)
            {
                // Check the text found by OCR
                isImageSafe = await CheckTextContent(ocrResult.Text);
            }
        }

        return isImageSafe;
    }
}