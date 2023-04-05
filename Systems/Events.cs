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
        _ = Task.Run(async () =>
        {
            var dict = new Dictionary<string, string>
            {
                { "guildId", user.Guild.Id.ToString() },
                { "userId", user.Id.ToString() }
            };
            await new Queue("FreshCheck", null, dict, DateTime.UtcNow.AddMinutes(35)).Insert();
        });
        return Task.CompletedTask;
    }

    private static Task MessageReceivedHandler(SocketMessage message)
    {
        _ = Task.Run(async () =>
        {
            if (message.Channel is SocketDMChannel && message.Author.MutualGuilds.Any() &&
                message.Author.Id != _Client.CurrentUser.Id)
            {
                //Get guilds
                var mutualGuilds = message.Author.MutualGuilds.Select(g => g.Id.ToString());
                //Create embed to send to guild
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
                    new EmbedBuilder().WithTitle(attachment.Filename).WithImageUrl(attachment.Url).WithUrl(attachment.Url)));
                //Confirm message and where to send
                var builder = new ComponentBuilder();
                builder.WithButton("CANCEL", "dmRecieved:cancel:cancel", ButtonStyle.Danger);
                foreach (var guild in message.Author.MutualGuilds)
                {
                    var guildId = guild.Id.ToString();
                    var guildName = guild.Name;
                    builder.WithButton(guildName, $"dmRecieved:{guildId}:{guildName}");
                }
                await message.Author.SendMessageAsync("This message will be sent to the Mod team, please select the server you would like to send it to",
                    embeds: embeds.Select(embed => embed.Build()).ToArray(), components: builder.Build());
            }
        });
        return Task.CompletedTask;
    }
}