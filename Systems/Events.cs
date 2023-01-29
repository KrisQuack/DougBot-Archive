using System.Text.Json;
using Discord;
using Discord.WebSocket;
using DougBot.Models;
using Fernandezja.ColorHashSharp;

namespace DougBot.Systems;

public class Events
{
    private readonly DiscordSocketClient _Client;

    public Events(DiscordSocketClient client)
    {
        _Client = client;
        client.MessageReceived += MessageReceivedHandler;
        client.UserJoined += UserJoinedHandler;
        Console.WriteLine("EventHandler Initialized");
    }

    private async Task UserJoinedHandler(SocketGuildUser user)
    {
        var dict = new Dictionary<string, string>
        {
            { "guildId", user.Guild.Id.ToString() },
            { "userId", user.Id.ToString() }
        };
        var roleJson = JsonSerializer.Serialize(dict);
        Queue.Create("FreshCheck", null, roleJson, DateTime.UtcNow.AddMinutes(11));
    }

    private async Task MessageReceivedHandler(SocketMessage message)
    {
        var settings = Setting.GetSettings();
        if (message.Channel is SocketDMChannel && message.Author.MutualGuilds.Any() &&
            message.Author.Id != _Client.CurrentUser.Id)
        {
            var embeds = new List<EmbedBuilder>();
            //Main embed
            var colorHash = new ColorHash();
            var color = colorHash.BuildToColor(message.Author.Id.ToString());
            embeds.Add(new EmbedBuilder()
                .WithDescription(message.Content)
                .WithColor((Color)color)
                .WithAuthor(new EmbedAuthorBuilder()
                    .WithName($"{message.Author.Username}#{message.Author.Discriminator} ({message.Author.Id})")
                    .WithIconUrl(message.Author.GetAvatarUrl()))
                .WithCurrentTimestamp());
            //Attachment embeds
            embeds.AddRange(message.Attachments.Select(attachment =>
                new EmbedBuilder().WithTitle(attachment.Filename).WithImageUrl(attachment.Url)
                    .WithUrl(attachment.Url)));
            var embedJson = JsonSerializer.Serialize(embeds,
                new JsonSerializerOptions { Converters = { new ColorJsonConverter() } });
            var dict = new Dictionary<string, string>
            {
                { "guildId", settings.guildID },
                { "channelId", settings.dmReceiptChannel },
                { "message", "" },
                { "embedBuilders", embedJson },
                { "ping", "false" }
            };
            var json = JsonSerializer.Serialize(dict);
            Queue.Create("SendMessage", null, json, DateTime.UtcNow);
        }
    }
}