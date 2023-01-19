using Discord;
using Discord.WebSocket;
using DougBot.Models;

namespace DougBot.Systems;

public static class AuditLog
{
    public static async Task<EmbedBuilder> LogEvent(DiscordSocketClient client, string content, bool status)
    {
        var settings = Setting.GetSettings();
        var color = status ? Color.Green : Color.Red;
        var embed = new EmbedBuilder()
            .WithDescription(content)
            .WithColor(color)
            .WithCurrentTimestamp();
        var guild = client.Guilds.FirstOrDefault(g => g.Id.ToString() == settings.guildID);
        var channel = guild.Channels.FirstOrDefault(c => c.Id.ToString() == settings.logChannel) as SocketTextChannel;
        await channel.SendMessageAsync(embed: embed.Build());
        return embed;
    }
}