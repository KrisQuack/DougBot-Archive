using TwitchLib.Api;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Clients;
using TwitchLib.Communication.Models;

namespace DougBot.Twitch;

public class IRC
{
    private TwitchAPI API;
    private string BotID;
    private string BotName;
    private string ChannelName;
    private TwitchClient Client;

    private readonly string[] containsBlock = { "-.", ".-" };
    private readonly string[] endsWithBlock = { "ussy" };

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
                //Check for blocked words
                if (containsBlock.Any(word => msg.Contains(word)) || endsWithBlock.Any(word => msg.EndsWith(word)))
                {
                    await API.Helix.Moderation.DeleteChatMessagesAsync(Message.ChatMessage.RoomId, BotID,
                        Message.ChatMessage.Id);
                    return;
                }

                //Check for spam
                var words = msg.Split(' ');
                foreach (var word in words.Distinct())
                    if (words.Count(w => w == word) > 5)
                        await API.Helix.Moderation.DeleteChatMessagesAsync(Message.ChatMessage.RoomId, BotID,
                            Message.ChatMessage.Id);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        });
    }

    private void Client_OnLog(object sender, OnLogArgs e)
    {
        if (!e.Data.Contains("PRIVMSG") && !e.Data.Contains("CLEARMSG") && !e.Data.Contains("USERNOTICE") &&
            !e.Data.Contains("CLEARCHAT") && !e.Data.Contains("PART") && !e.Data.Contains("JOIN"))
            Console.WriteLine($"{DateTime.UtcNow.ToString()} {e.Data}");
    }

    private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs Channel)
    {
        Console.WriteLine($"Joined {Channel.Channel}");
    }
}