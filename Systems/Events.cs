using System.Text.Json;
using Discord;
using Discord.WebSocket;
using DougBot.Models;
using Fernandezja.ColorHashSharp;

namespace DougBot.Systems;

public static class Events
{
    private static DiscordSocketClient _Client;

    public static async Task Monitor(DiscordSocketClient client)
    {
        _Client = client;
        client.MessageReceived += MessageReceivedHandler;
        client.UserJoined += UserJoinedHandler;
        Console.WriteLine("EventHandler Initialized");
    }

    private static Task UserJoinedHandler(SocketGuildUser user)
    {
        _ = Task.Run(async () => {
        var dict = new Dictionary<string, string>
        {
            { "guildId", user.Guild.Id.ToString() },
            { "userId", user.Id.ToString() }
        };
        await new Queue("FreshCheck", null, dict, DateTime.UtcNow.AddMinutes(10)).Insert();
        });
        return Task.CompletedTask;
    }

    private static Task MessageReceivedHandler(SocketMessage message)
    {
        _ = Task.Run(async () => {
        if (message.Channel is SocketDMChannel && message.Author.MutualGuilds.Any() &&
            message.Author.Id != _Client.CurrentUser.Id)
        {
            //Get guilds
            var mutualGuilds = message.Author.MutualGuilds.Select(g => g.Id.ToString());
            var dbGuilds = await Guild.GetGuilds();
            dbGuilds = dbGuilds.Where(g => mutualGuilds.Contains(g.Id)).ToList();
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
                    { "ping", "false" },
                    { "attachments", null }
                };
                await new Queue("SendMessage", null, dict, null).Insert();
            }
        }
        });
        return Task.CompletedTask;
    }
}