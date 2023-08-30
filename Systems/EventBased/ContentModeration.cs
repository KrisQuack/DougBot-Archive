using System.Collections.Concurrent;
using System.Text;
using Discord;
using Discord.WebSocket;
using DougBot.Models;
using Microsoft.Azure.CognitiveServices.ContentModerator;
using Microsoft.Azure.CognitiveServices.ContentModerator.Models;
using OpenAI.ObjectModels.RequestModels;

namespace DougBot.Systems.EventBased;

public static class ContentModeration
{
    private static DiscordSocketClient _client;
    private static readonly BlockingCollection<SocketMessage> _messagesToModerate = new();
    private static readonly ConcurrentDictionary<ulong, List<DateTime>> _channelFlags = new();

    private static readonly ContentModeratorClient _moderatorClient =
        new(new ApiKeyServiceClientCredentials(ConfigurationService.Instance.ContentModerationToken))
        {
            Endpoint = ConfigurationService.Instance.ContentModerationUrl
        };

    public static async Task Monitor()
    {
        _client = Program.Client;
        _client.MessageReceived += MessageReceivedHandler;
        _client.MessageUpdated += MessageUpdatedHandler;
        _ = ProcessMessages();
        _ = CleanUpOldFlags();
        Console.WriteLine("Content Moderation Initialized");
    }

    private static Task MessageUpdatedHandler(Cacheable<IMessage, ulong> cacheable, SocketMessage message,
        ISocketMessageChannel channel)
    {
        //Check if _guild is null and if so if this message is valid
        var guildChannel = channel as SocketTextChannel;
        if (guildChannel == null || message.Author.IsBot ||
            ConfigurationService.Instance.LogBlacklistChannels.Contains(guildChannel) ||
            ConfigurationService.Instance.LogBlacklistChannels.Contains(guildChannel)
           ) return Task.CompletedTask;
        //Process
        _messagesToModerate.Add(message);
        return Task.CompletedTask;
    }

    private static async Task MessageReceivedHandler(SocketMessage message)
    {
        //Author
        var author = message.Author as SocketGuildUser;
        //Check if _guild is null and if so if this message is valid
        var guildChannel = message.Channel as SocketTextChannel;
        if (guildChannel == null || message.Author.IsBot ||
            ConfigurationService.Instance.LogBlacklistChannels.Contains(guildChannel) ||
            ConfigurationService.Instance.LogBlacklistChannels.Contains(guildChannel) ||
            author.Roles.Any(r => r.Id == 718291172124131408)
           ) return;
        //Check for headers by splitting it into lines and checking if any start with "# ", "## ", or "### "
        var lines = message.Content.Split("\n");
        if (lines.Any(l => l.StartsWith("# ")) || lines.Any(l => l.StartsWith("## ")) ||
            lines.Any(l => l.StartsWith("### "))) await message.DeleteAsync();
        //Process
        _messagesToModerate.Add(message);
    }

    private static Task ProcessMessages()
    {
        _ = Task.Run(async () =>
        {
            foreach (var message in _messagesToModerate.GetConsumingEnumerable())
                try
                {
                    //Check if the message is a reply
                    IEnumerable<IMessage> messageContext;
                    //Check if the text is safe
                    messageContext = await message.Channel.GetMessagesAsync(20).FlattenAsync();
                    messageContext = messageContext.Where(m => !string.IsNullOrEmpty(m.CleanContent));
                    messageContext = messageContext.OrderBy(m => m.CreatedAt);
                    var (isTextSafe, textResponse) = await CheckTextContent(message.CleanContent, messageContext);
                    if (!isTextSafe) await SendModerationEmbed(messageContext, textResponse, false);
                }
                catch (Exception e)
                {
                    if (e.Message.Contains("BadRequest")) continue;
                    Console.WriteLine($"[General/Warning] {DateTime.UtcNow:HH:mm:ss} ContentModerator {e}");
                }
        });
        return Task.CompletedTask;
    }

    private static async Task<(bool, string)> CheckTextContent(string text,
        IEnumerable<IMessage>? messageContext = null)
    {
        //Check if the text is empty
        if (string.IsNullOrWhiteSpace(text)) return (true, "");
        //Check with Azure content mod
        await Task.Delay(1000);
        var screenResult = await _moderatorClient.TextModeration.ScreenTextAsync("text/plain",
            new MemoryStream(Encoding.UTF8.GetBytes(text)), "eng", classify: true, pII: true);
        if (screenResult.Terms == null && !text.Contains("?weird")) return (true, "");
        if (messageContext == null) return (false, string.Join(", ", screenResult.Terms.Select(t => t.Term)));
        //Check if there has been more than 5 flags in the last 5 minutes
        var now = DateTime.UtcNow;
        var message = messageContext.Last();
        _channelFlags.AddOrUpdate(
            message.Channel.Id,
            new List<DateTime> { now },
            (_, existing) =>
            {
                existing.Add(now);
                return existing;
            }
        );
        var recentFlags = _channelFlags[message.Channel.Id].Where(time => now - time <= TimeSpan.FromMinutes(10))
            .ToList();
        if (recentFlags.Count < 5 && !text.Contains("?weird")) return (true, "");
        _channelFlags.TryRemove(message.Channel.Id, out _);
        //If bad word, Ask AI if it is safe
        var contextResponse = await CheckTextContext(messageContext);
        var isContextSafe = contextResponse == "No";
        //Return if the message is safe
        return (isContextSafe, contextResponse);
    }

    private static async Task<string> CheckTextContext(IEnumerable<IMessage> messageContext)
    {
        var chatMessages = new List<ChatMessage>
        {
            ChatMessage.FromSystem("You are acting as a content filter for a discord server. " +
                                   "You will analyzing chat provided to you and determining if it violates any rules. " +
                                   "Obvious jokes or light hearted comments and perfectly acceptable and will not need flagging. " +
                                   "Mild sexual refference and jokes are also acceptable as long as they are not over the line. " +
                                   "If a rule is broken, respond with the violation. If no rule is broken, respond only with 'No'.\n" +
                                   "Rules: Follow Discord's TOS, No Offensive Speech, Be Kind, No Spam, English Only, No Impersonation, No Political/Sexual/Distressing Topics."),
            ChatMessage.FromUser(
                "<@1>:Hey guys in going to live garden today\n<@2>:I fucking love olive garden\n<@1>:haha me too thats great"),
            ChatMessage.FromAssistant("No"),
            ChatMessage.FromUser("<@1>:Hey how is everyone today\r\n<@2>:I am good\r\n<@3>:Fuck you, you dont know me"),
            ChatMessage.FromSystem("<@3> Rude behaviour towards <@1>"),
            ChatMessage.FromUser(string.Join("\n", messageContext.Select(m => $"{m.Author.Mention}:{m.CleanContent}")))
        };
        return await OpenAIGPT.Gpt48k(chatMessages);
    }

    private static async Task SendModerationEmbed(IEnumerable<IMessage> messageContext, string reason, bool image)
    {
        var message = messageContext.LastOrDefault();
        var contextString = string.Join("\n", messageContext.Select(m => $"{m.Author.GlobalName}: {m.CleanContent}"));
        var contextTruncated = contextString.Length <= 1024
            ? contextString
            : "..." + contextString.Substring(contextString.Length - 1020);
        // Create the main embed
        var embed = new EmbedBuilder();
        embed.Title = "Content Moderation: " + (image ? "Bad Image" : "Problematic Conversation");
        embed.Url = message.GetJumpUrl();
        embed.AddField("Author", message.Author.Mention, true);
        embed.AddField("Channel", ((ITextChannel)message.Channel).Mention, true);
        embed.AddField("Content", image ? "Media" : contextTruncated);
        embed.AddField("Reason", reason);
        //Create a list of attachments
        if (image)
            foreach (var attachment in message.Attachments)
                embed.ImageUrl = attachment.Url;
        // Send the embed to the log channel.
        var logChannel = ConfigurationService.Instance.Guild.GetTextChannel(571965793131036672);
        await logChannel.SendMessageAsync(embed: embed.Build());
    }

    public static async Task CleanUpOldFlags()
    {
        while (true)
        {
            try
            {
                // Remove all flags older than 10 minutes
                var now = DateTime.UtcNow;
                foreach (var key in _channelFlags.Keys)
                    _channelFlags.AddOrUpdate(
                        key,
                        new List<DateTime>(),
                        (_, existing) =>
                        {
                            return existing.Where(time => now - time <= TimeSpan.FromMinutes(10)).ToList();
                        }
                    );
            }
            catch (Exception e)
            {
                Console.WriteLine($"[General/Warning] {DateTime.UtcNow:HH:mm:ss} CleanUpOldFlags {e}");
            }

            // Wait some time before the next cleanup
            await Task.Delay(TimeSpan.FromMinutes(5));
        }
    }
}