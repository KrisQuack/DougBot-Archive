using Discord;
using DougBot.Models;
using DougBot.Scheduler;
using TwitchLib.Client;
using TwitchLib.Client.Events;
using TwitchLib.Client.Models;
using TwitchLib.Communication.Events;

namespace DougBot.Systems.Twitch;

public class Irc
{
    private readonly string _botId = "853660174";
    private string[] _blockedWords;
    private string[] _containsBlock;
    private string[] _endsWithBlock;
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
        UpdateBlocks();
        return client;
    }

    public async Task UpdateBlocks()
    {
        Console.WriteLine("ReactionFilter Initialized");
        while (true)
            try
            {
                var dbGuild = await Guild.GetGuild("567141138021089308");
                _containsBlock = dbGuild.TwitchContainsBlock;
                _blockedWords = dbGuild.TwitchBlockedWords;
                _endsWithBlock = dbGuild.TwitchEndsWithBlock;
                _firstRun = true;
                await Task.Delay(60000);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
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
                //Check for blocked contains
                if (_containsBlock.Any(word => msg.Contains(word)) || _endsWithBlock.Any(word => msg.EndsWith(word)))
                {
                    await Twitch.Api.Helix.Moderation.DeleteChatMessagesAsync(message.ChatMessage.RoomId, _botId,
                        message.ChatMessage.Id);
                    return;
                }

                var words = msg.Split(' ');
                //Check for blocked words
                if (words.Any(word => _blockedWords.Contains(word)))
                {
                    await Twitch.Api.Helix.Moderation.DeleteChatMessagesAsync(message.ChatMessage.RoomId, _botId,
                        message.ChatMessage.Id);
                    return;
                }

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
            await SendMessageJob.Queue("567141138021089308", "1080251555619557445", new List<EmbedBuilder> { embed },
                DateTime.UtcNow);
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