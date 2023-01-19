using System.Text.Json;
using Discord;
using Discord.WebSocket;
using DougBot.Models;

namespace DougBot.Scheduler;

public static class Message
{
    public static async Task Send(DiscordSocketClient client, ulong guidID, ulong channelId, string message,
        string embedBuilders)
    {
        var guild = client.GetGuild(guidID);
        var channel = guild.Channels.FirstOrDefault(x => x.Id == channelId) as SocketTextChannel;
        var embeds = new List<Embed>();
        foreach (var embed in JsonSerializer.Deserialize<List<EmbedBuilder>>(embedBuilders))
            embeds.Add(embed.Build());
        await channel.SendMessageAsync(message, embeds: embeds.ToArray());
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
        var embeds = JsonSerializer.Deserialize<List<EmbedBuilder>>(embedBuilders).Select(embed => embed.Build())
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
        embeds = JsonSerializer.Deserialize<List<EmbedBuilder>>(embedBuilders).Select(embed =>
            embed.WithTitle(Status)
                .WithColor(color)
                .WithAuthor($"DM to {user.Username}#{user.Discriminator} ({user.Id}) from {sender.Username}",
                    sender.GetAvatarUrl())
                .Build()).ToList();
        await channel.SendMessageAsync(embeds: embeds.ToArray());
    }
}