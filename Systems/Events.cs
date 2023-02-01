using System.Text.Json;
using Discord;
using Discord.WebSocket;
using DougBot.Models;
using Fernandezja.ColorHashSharp;

namespace DougBot.Systems;

public class Events
{
    private static DiscordSocketClient _Client;

    public static async Task Monitor(DiscordSocketClient client)
    {
        _Client = client;
        client.MessageReceived += MessageReceivedHandler;
        client.UserJoined += UserJoinedHandler;
        Console.WriteLine("EventHandler Initialized");
    }

    private static async Task UserJoinedHandler(SocketGuildUser user)
    {
        var dict = new Dictionary<string, string>
        {
            { "guildId", user.Guild.Id.ToString() },
            { "userId", user.Id.ToString() }
        };
        var json = JsonSerializer.Serialize(dict);
        var queue = new Queue()
        {
            Id = Guid.NewGuid().ToString(),
            Type = "FreshCheck",
            Keys = json,
            DueAt = DateTime.UtcNow.AddMinutes(10)
        };
        await using var db = new Database.DougBotContext();
        await db.Queues.AddAsync(queue);
        await db.SaveChangesAsync();
    }

    private static async Task MessageReceivedHandler(SocketMessage message)
    {
        if (message.Channel is SocketDMChannel && message.Author.MutualGuilds.Any() &&
            message.Author.Id != _Client.CurrentUser.Id)
        {
            //Get guilds
            await using var db = new Database.DougBotContext();
            var mutualGuilds = message.Author.MutualGuilds.Select(g => g.Id.ToString());
            var dbGuilds = db.Guilds.Where(g => mutualGuilds.Contains(g.Id));
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
            //Send message to each guild the user is in
            foreach (var dbGuild in dbGuilds)
            {
                var dict = new Dictionary<string, string>
                {
                    { "guildId", dbGuild.Id },
                    { "channelId", dbGuild.DmReceiptChannel },
                    { "message", "" },
                    { "embedBuilders", embedJson },
                    { "ping", "false" }
                };
                var json = JsonSerializer.Serialize(dict);
                var queue = new Queue()
                {
                    Id = Guid.NewGuid().ToString(),
                    Type = "SendMessage",
                    Keys = json
                };
                await db.Queues.AddAsync(queue);
            }
            await db.SaveChangesAsync();
        }
    }
}