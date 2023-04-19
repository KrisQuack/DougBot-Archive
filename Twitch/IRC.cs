using DougBot.Models;
using TwitchLib.Api;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace DougBot.Twitch;

public class IRC
{
    private string[] containsBlock;
    private string[] blockedWords;
    private string[] endsWithBlock;
    private bool initialized = false;
    private string BotID;
    private TwitchClient Client;

    public TwitchClient Initialize(string botID)
    {
        BotID = botID;
        var clientOptions = new ClientOptions
        {
            MessagesAllowedInPeriod = 750,
            ThrottlingPeriod = TimeSpan.FromSeconds(30)
        };
        var customClient = new WebSocketClient(clientOptions);
        Client = new TwitchClient(customClient);
        Client.OnLog += Client_OnLog;
        Client.OnJoinedChannel += Client_OnJoinedChannel;
        Client.OnMessageReceived += Client_OnMessageReceived;
        UpdateBlocks();
        return Client;
    }

    public async Task UpdateBlocks()
    {
        Console.WriteLine("ReactionFilter Initialized");
        while (true)
        {
            try
            {
                var dbGuild = await Guild.GetGuild("567141138021089308");
                containsBlock = dbGuild.TwitchSettings.ContainsBlock;
                blockedWords = dbGuild.TwitchSettings.BlockedWords;
                endsWithBlock = dbGuild.TwitchSettings.EndsWithBlock;
                initialized = true;
                await Task.Delay(60000);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }

    private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs Message)
    {
        //Skip mods and broadcaster
        if (!initialized || Message.ChatMessage.IsModerator || Message.ChatMessage.IsBroadcaster ||Message.ChatMessage.Bits > 0) return;
        //Process
        _ = Task.Run(async () =>
        {
            try
            {
                var msg = Message.ChatMessage.Message.ToLower();
                //Check for blocked contains
                if (containsBlock.Any(word => msg.Contains(word)) || endsWithBlock.Any(word => msg.EndsWith(word)))
                {
                    await Twitch.API.Helix.Moderation.DeleteChatMessagesAsync(Message.ChatMessage.RoomId, BotID,
                        Message.ChatMessage.Id);
                    return;
                }

                var words = msg.Split(' ');
                //Check for blocked words
                if (words.Any(word => blockedWords.Contains(word)))
                {
                    await Twitch.API.Helix.Moderation.DeleteChatMessagesAsync(Message.ChatMessage.RoomId, BotID,Message.ChatMessage.Id);
                    return;
                }

                //Check for spam

                foreach (var word in words.Distinct())
                {
                    if (words.Count(w => w == word) > 10)
                    {
                        await Twitch.API.Helix.Moderation.DeleteChatMessagesAsync(Message.ChatMessage.RoomId, BotID, Message.ChatMessage.Id);
                        return;
                    }
                }
                        
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        });
    }

    private void Client_OnLog(object sender, OnLogArgs e)
    {
        var keywords = new List<string>
            { "PRIVMSG", "CLEARMSG", "USERNOTICE", "CLEARCHAT", "PART", "JOIN", "PING", "PONG" };

        if (!keywords.Any(keyword => e.Data.Contains(keyword)))
            Console.WriteLine($"{DateTime.UtcNow:u} {e.Data}");
    }

    private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs Channel)
    {
        Console.WriteLine($"Joined {Channel.Channel}");
    }
}