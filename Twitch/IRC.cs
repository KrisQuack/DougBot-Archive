using TwitchLib.Api;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace DougBot.Twitch;

public class IRC
{
    private readonly string[] containsBlock = { "-.", ".-" };
    private readonly string[] blockedWords = { "a" };
    private readonly string[] endsWithBlock = { "ussy" };
    private TwitchAPI API;
    private string BotID;
    private string BotName;
    private string ChannelName;
    private TwitchClient Client;

    public TwitchClient Initialize(TwitchAPI api, string botID, string botName, string channelName)
    {
        API = api;
        BotID = botID;
        BotName = botName;
        ChannelName = channelName;
        var credentials = new ConnectionCredentials("justinfan7011", "", disableUsernameCheck: true);
        var clientOptions = new ClientOptions
        {
            MessagesAllowedInPeriod = 750,
            ThrottlingPeriod = TimeSpan.FromSeconds(30)
        };
        var customClient = new WebSocketClient(clientOptions);
        Client = new TwitchClient(customClient);
        Client.Initialize(credentials, channelName);

        Client.OnLog += Client_OnLog;
        Client.OnJoinedChannel += Client_OnJoinedChannel;
        Client.OnMessageReceived += Client_OnMessageReceived;

        Client.Connect();
        return Client;
    }

    private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs Message)
    {
        //Skip mods and broadcaster
        if (Message.ChatMessage.IsModerator || Message.ChatMessage.IsBroadcaster ||
            Message.ChatMessage.Bits > 0) return;
        //Process
        _ = Task.Run(async () =>
        {
            try
            {
                var msg = Message.ChatMessage.Message.ToLower();
                //Check for blocked contains
                if (containsBlock.Any(word => msg.Contains(word)) || endsWithBlock.Any(word => msg.EndsWith(word)))
                {
                    await API.Helix.Moderation.DeleteChatMessagesAsync(Message.ChatMessage.RoomId, BotID,
                        Message.ChatMessage.Id);
                    return;
                }

                var words = msg.Split(' ');
                //Check for blocked words
                if (words.Any(word => blockedWords.Contains(word)))
                {
                    await API.Helix.Moderation.DeleteChatMessagesAsync(Message.ChatMessage.RoomId, BotID,Message.ChatMessage.Id);
                    return;
                }

                //Check for spam

                foreach (var word in words.Distinct())
                {
                    if (words.Count(w => w == word) > 10)
                    {
                        await API.Helix.Moderation.DeleteChatMessagesAsync(Message.ChatMessage.RoomId, BotID, Message.ChatMessage.Id);
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