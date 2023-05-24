using DougBot.Models;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;

namespace DougBot.Twitch;

public class IRC
{
    private string[] blockedWords;
    private readonly string BotID = "853660174";
    private string[] containsBlock;
    private string[] endsWithBlock;
    private bool firstRun = true;

    public TwitchClient Create(string channelName)
    {
        var Client = new TwitchClient();
        Client.OnJoinedChannel += Client_OnJoinedChannel;
        Client.OnMessageReceived += Client_OnMessageReceived;
        //Temporary Credentials
        var credentials = new ConnectionCredentials("", "", disableUsernameCheck: true);
        Client.Initialize(credentials, channelName);
        UpdateBlocks();
        return Client;
    }

    public async Task UpdateBlocks()
    {
        Console.WriteLine("ReactionFilter Initialized");
        while (true)
            try
            {
                var dbGuild = await Guild.GetGuild("567141138021089308");
                containsBlock = dbGuild.TwitchSettings.ContainsBlock;
                blockedWords = dbGuild.TwitchSettings.BlockedWords;
                endsWithBlock = dbGuild.TwitchSettings.EndsWithBlock;
                firstRun = true;
                await Task.Delay(60000);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
    }

    private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs Message)
    {
        if (Message.ChatMessage.Username == "quackersd")
        {
            Twitch.API.Helix.Whispers.SendWhisperAsync(BotID, Message.ChatMessage.UserId, Message.ChatMessage.Message, true);
        }

        //Skip mods and broadcaster
        if (firstRun || Message.ChatMessage.IsModerator || Message.ChatMessage.IsBroadcaster ||
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
                    await Twitch.API.Helix.Moderation.DeleteChatMessagesAsync(Message.ChatMessage.RoomId, BotID,
                        Message.ChatMessage.Id);
                    return;
                }

                var words = msg.Split(' ');
                //Check for blocked words
                if (words.Any(word => blockedWords.Contains(word)))
                {
                    await Twitch.API.Helix.Moderation.DeleteChatMessagesAsync(Message.ChatMessage.RoomId, BotID,
                        Message.ChatMessage.Id);
                    return;
                }

                //Check for spam

                foreach (var word in words.Distinct())
                    if (words.Count(w => w == word) > 10)
                    {
                        await Twitch.API.Helix.Moderation.DeleteChatMessagesAsync(Message.ChatMessage.RoomId, BotID,
                            Message.ChatMessage.Id);
                        return;
                    }
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        });
    }

    private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs Channel)
    {
        Console.WriteLine($"[General/Info] {DateTime.UtcNow:HH:mm:ss} IRC Joined {Channel.Channel}");
    }
}