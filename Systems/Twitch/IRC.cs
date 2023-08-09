using Discord;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Events;

namespace DougBot.Systems.Twitch;

public class Irc
{
    private readonly string _botId = "853660174";
    private bool _firstRun = true;

    public TwitchClient Create(string channelName)
    {
        var client = new TwitchClient();
        client.OnJoinedChannel += Client_OnJoinedChannel;
        client.OnMessageReceived += Client_OnMessageReceived;
        client.OnWhisperReceived += Client_OnWhisperReceived;
        client.OnError += Client_OnError;
        //Temporary Credentials
        var credentials = new ConnectionCredentials("", "", disableUsernameCheck: true);
        client.Initialize(credentials, channelName);
        return client;
    }

    private void Client_OnMessageReceived(object sender, OnMessageReceivedArgs message)
    {
        if (message.ChatMessage.IsModerator && message.ChatMessage.Message.ToLower().Contains("wah, you up?"))
            Twitch.Irc.SendMessage(message.ChatMessage.Channel,
                $"@{message.ChatMessage.Username} Let me sleep, I'm tired");
        //Skip mods and broadcaster
        if (_firstRun || message.ChatMessage.IsModerator || message.ChatMessage.IsBroadcaster ||
            message.ChatMessage.Bits > 0) return;
        //Process
        _ = Task.Run(async () =>
        {
            try
            {
                var msg = message.ChatMessage.Message.ToLower();
                var words = msg.Split(' ');
                //Check for spam
                if (words.Distinct().Any(word => words.Count(w => w == word) > 10))
                    await Twitch.Api.Helix.Moderation.DeleteChatMessagesAsync(message.ChatMessage.RoomId, _botId,
                        message.ChatMessage.Id);
            }
            catch (Exception e)
            {
                Console.WriteLine(e);
            }
        });
    }

    private void Client_OnWhisperReceived(object? sender, OnWhisperReceivedArgs whisper)
    {
        _ = Task.Run(async () =>
        {
            var embed = new EmbedBuilder()
                .WithTitle($"New Twitch DM from {whisper.WhisperMessage.DisplayName}")
                .WithDescription(whisper.WhisperMessage.Message)
                .WithColor(Color.Purple)
                .WithCurrentTimestamp();
            await ConfigurationService.Instance.Guild.GetTextChannel(1080251555619557445).
            SendMessageAsync(embed: embed.Build());
        });
    }

    private void Client_OnJoinedChannel(object sender, OnJoinedChannelArgs channel)
    {
        Console.WriteLine($"[General/Info] {DateTime.UtcNow:HH:mm:ss} IRC Joined {channel.Channel}");
    }

    private void Client_OnError(object? sender, OnErrorEventArgs e)
    {
        Console.WriteLine($"[General/Warning] {DateTime.UtcNow:HH:mm:ss} TwitchIRC {e}");
    }
}