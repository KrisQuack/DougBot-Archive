using System.Text.Json;
using Discord;
using Discord.WebSocket;
using DougBot.Models;
using Exception = System.Exception;

namespace DougBot.Scheduler;

public static class Message
{
    public static async Task Send(DiscordSocketClient client, ulong guidID, ulong channelId, string message,
        string embedBuilders, bool ping = false)
    {
        var guild = client.GetGuild(guidID);
        var channel = guild.Channels.FirstOrDefault(x => x.Id == channelId) as SocketTextChannel;
        var embedBuildersList = JsonSerializer.Deserialize<List<EmbedBuilder>>(embedBuilders,
            new JsonSerializerOptions { Converters = { new ColorJsonConverter() } });
        await channel.SendMessageAsync(message, embeds: embedBuildersList.Select(embed => embed.Build()).ToArray(),
            allowedMentions: ping ? AllowedMentions.All : AllowedMentions.None);
    }

    public static async Task SendDM(DiscordSocketClient client, ulong userId, ulong senderId, string embedBuilders)
    {
        var settings = Setting.GetSettings();
        var guild = client.Guilds.FirstOrDefault(g => g.Id.ToString() == settings.guildID);
        var channel =
            guild.Channels.FirstOrDefault(c => c.Id.ToString() == settings.dmReceiptChannel) as SocketTextChannel;
        var user = await client.GetUserAsync(userId);
        var sender = await client.GetUserAsync(senderId);
        //Send user DM
        var embeds = JsonSerializer
            .Deserialize<List<EmbedBuilder>>(embedBuilders,
                new JsonSerializerOptions { Converters = { new ColorJsonConverter() } }).Select(embed => embed.Build())
            .ToList();
        var Status = "";
        var color = (Color)embeds[0].Color;
        try
        {
            await user.SendMessageAsync(embeds: embeds.ToArray());
            Status = "Message Delivered";
            color = Color.Green;
        }
        catch (Exception ex)
        {
            if (ex.Message.Contains("Cannot send messages to this user"))
                Status = "User has blocked DMs";
            else
                Status = "Error: " + ex.Message;
            color = Color.Red;
        }

        //Send status to mod channel
        embeds = JsonSerializer.Deserialize<List<EmbedBuilder>>(embedBuilders,
            new JsonSerializerOptions { Converters = { new ColorJsonConverter() } }).Select(embed =>
            embed.WithTitle(Status)
                .WithColor(color)
                .WithAuthor($"DM to {user.Username}#{user.Discriminator} ({user.Id}) from {sender.Username}",
                    sender.GetAvatarUrl())
                .Build()).ToList();
        await channel.SendMessageAsync(embeds: embeds.ToArray());
    }
}