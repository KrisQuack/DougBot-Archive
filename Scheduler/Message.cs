using System.Text.Json;
using Discord;
using Discord.WebSocket;
using DougBot.Models;
using Fernandezja.ColorHashSharp;

namespace DougBot.Scheduler;

public static class Message
{
    public static async Task Send(DiscordSocketClient client, ulong GuildId, ulong ChannelId, string Message,
        string EmbedBuilders, bool Ping = false, string attachments = null)
    {
        //Get the guild and channel
        var guild = client.GetGuild(GuildId);
        var channel = guild.Channels.FirstOrDefault(x => x.Id == ChannelId) as SocketTextChannel;
        //Deserialize the embeds
        var embedBuildersList = JsonSerializer.Deserialize<List<EmbedBuilder>>(EmbedBuilders,
            new JsonSerializerOptions { Converters = { new ColorJsonConverter() } });
        //If embeds have attachment url also add it as the image url
        foreach (var embed in embedBuildersList.Where(embed => embed.Url != null && embed.ImageUrl == null))
            embed.WithImageUrl(embed.Url);
        //Send the message
        await channel.SendMessageAsync(Message, embeds: embedBuildersList.Select(embed => embed.Build()).ToArray(),
            allowedMentions: Ping ? AllowedMentions.All : AllowedMentions.None);
        //Send the attachments
        if (attachments != null)
        {
            var attachList = JsonSerializer.Deserialize<List<string>>(attachments);
            foreach (var attachment in attachList)
            {
                await channel.SendFileAsync(attachment, attachment.Split('/').LastOrDefault(x => x.Contains('.')));
                //delete file
                File.Delete(attachment);
            }
        }
    }

    public static async Task SendDM(DiscordSocketClient client, ulong GuildId, ulong UserId, ulong SenderId,
        string EmbedBuilders)
    {
        //Get the guild settings
        var dbGuild = await Guild.GetGuild(GuildId.ToString());
        //Get the guild, channel, user and sender
        var guild = client.Guilds.FirstOrDefault(g => g.Id == GuildId);
        var channel =
            guild.Channels.FirstOrDefault(c => c.Id.ToString() == dbGuild.DmReceiptChannel) as SocketTextChannel;
        var user = await client.GetUserAsync(UserId);
        var sender = await client.GetUserAsync(SenderId);
        //Deserialize the embeds
        var embeds = JsonSerializer
            .Deserialize<List<EmbedBuilder>>(EmbedBuilders,
                new JsonSerializerOptions { Converters = { new ColorJsonConverter() } }).Select(embed => embed.Build())
            .ToList();
        //Try and send the message and set the color and status
        var Status = "";
        var color = (Color)embeds[0].Color;
        var colorHash = new ColorHash();
        try
        {
            await user.SendMessageAsync(embeds: embeds.ToArray());
            Status = "Message Delivered";
            color = (Color)colorHash.BuildToColor(UserId.ToString());
            ;
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
        embeds = JsonSerializer.Deserialize<List<EmbedBuilder>>(EmbedBuilders,
            new JsonSerializerOptions { Converters = { new ColorJsonConverter() } }).Select(embed =>
            embed.WithTitle(Status)
                .WithColor(color)
                .WithAuthor($"DM to {user.Username}#{user.Discriminator} ({user.Id}) from {sender.Username}",
                    sender.GetAvatarUrl())
                .Build()).ToList();
        await channel.SendMessageAsync(embeds: embeds.ToArray());
    }
}