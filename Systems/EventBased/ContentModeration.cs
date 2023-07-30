using Discord;
using Discord.WebSocket;
using Microsoft.Azure.CognitiveServices.ContentModerator;
using Microsoft.Azure.CognitiveServices.ContentModerator.Models;
using System.Text;
using System.Collections.Concurrent;
using DougBot.Models;
using DougBot.Scheduler;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.Managers;
using OpenAI;

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
        _client.MessageUpdated += MessageUpdatedHandler;
        _ = ProcessMessages();
        Console.WriteLine("Content Moderation Initialized");
    }

    private static Task MessageUpdatedHandler(Cacheable<IMessage, ulong> cacheable, SocketMessage message, ISocketMessageChannel channel)
    {
        //Ignore bots
        if (message.Author.IsBot) return Task.CompletedTask;
        //Process
        _messagesToModerate.Add(message);
        return Task.CompletedTask;
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
                    IEnumerable<IMessage> messageContext;
                    //Check if there are any images and if they are safe
                    foreach (var attachment in message.Attachments)
                    {
                        var (isImageSafe, imageResponse) = await CheckImageContent(attachment.Url);
                        if (!isImageSafe)
                        {
                            messageContext = new List<IMessage> { message };
                            await SendModerationEmbed(messageContext, imageResponse, true);
                            continue;
                        }
                    }
                    //Check if the text is safe
                    messageContext = await message.Channel.GetMessagesAsync(10).FlattenAsync();
                    messageContext = messageContext.Where(m => !string.IsNullOrEmpty(m.CleanContent));
                    messageContext = messageContext.OrderBy(m => m.CreatedAt);
                    var (isTextSafe, textResponse) = await CheckTextContent(message.CleanContent, messageContext);
                    if (!isTextSafe)
                    {
                        await SendModerationEmbed(messageContext, textResponse, false);
                        continue;
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

    private static async Task<(bool,string)> CheckTextContent(string text, IEnumerable<IMessage>? messageContext =  null)
    {
        //Check if the text is empty
        if (string.IsNullOrWhiteSpace(text))
        {
            return (true,"");
        }
        //Check with Azure content mod
        await Task.Delay(1000);
        var screenResult = await _moderatorClient.TextModeration.ScreenTextAsync("text/plain", new MemoryStream(Encoding.UTF8.GetBytes(text)), language: "eng", classify: true, pII: true);
        if(screenResult.Terms == null) return (true,"");
        if(messageContext == null) return (false, string.Join(", ", screenResult.Terms.Select(t => t.Term)));
        //If bad word, setup OpenAI
        var openAiService = new OpenAIService(new OpenAiOptions()
        {
            ApiKey = Environment.GetEnvironmentVariable("AI_TOKEN")
        });
        //Check if the message context is safe
        var moderationResponse = await openAiService.Moderation.CreateModeration(new CreateModerationRequest()
        {
            Input = string.Join("\n", messageContext.Select(m => $"{m.Author.GlobalName}:{m.CleanContent}"))
        });
        var moderationResult = moderationResponse.Results.FirstOrDefault();
        //Make a string of categories that are flagged
        List<string> flags = new List<string>();
        if(moderationResult.Categories.Sexual) flags.Add("Sexual");
        if(moderationResult.Categories.SexualMinors) flags.Add("SexualMinors");
        if(moderationResult.Categories.Hate) flags.Add("Hate");
        if(moderationResult.Categories.HateThreatening) flags.Add("HateThreatening");
        if(moderationResult.Categories.SelfHarm) flags.Add("SelfHarm");
        if(moderationResult.Categories.Violence) flags.Add("Violence");
        if (moderationResult.Categories.ViolenceGraphic) flags.Add("ViolenceGraphic");
        //Return if the message is safe
        return (!moderationResult.Flagged, string.Join(", ", flags));
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
                var (isTextSafe, response) = await CheckTextContent(ocrResult.Text);
                if (!isTextSafe)
                {
                    isImageSafe = false;
                    reason = $"Offensive text detected in image: {response}";
                }
            }
        }

        return (isImageSafe, reason);
    }

    private static async Task SendModerationEmbed(IEnumerable<IMessage> messageContext, string reason, bool image)
    {
        var message = messageContext.LastOrDefault();
        var contextString = string.Join("\n", messageContext.Select(m => $"{m.Author.GlobalName}: {m.CleanContent}"));
        var contextTruncated = contextString.Length <= 1024 ? contextString : "..." + contextString.Substring(contextString.Length - 1020);
        // Create the main embed
        var embed = new EmbedBuilder();
        embed.Title = "Content Moderation: " + (image ? "Bad Image" : "Problematic Conversation");
        embed.Url = message.GetJumpUrl();
        embed.AddField("Author", message.Author.Mention, inline: true);
        embed.AddField("Channel", ((ITextChannel)message.Channel).Mention, inline: true);
        embed.AddField("Content", image ? "Media" : contextTruncated);
        embed.AddField("Reason", reason);
        //Create a list of attachments
        if (image)
        {
            foreach (var attachment in message.Attachments)
            {
                embed.ImageUrl = attachment.Url;
            }
        }
        // Send the embed to the log channel.
        await SendMessageJob.Queue("567141138021089308", "886548334154760242", new List<EmbedBuilder> { embed }, DateTime.UtcNow);
    }

}