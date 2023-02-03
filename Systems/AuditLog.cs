using System.Text.Json;
using Discord;
using DougBot.Models;

namespace DougBot.Systems;

public static class AuditLog
{
    public static async Task LogEvent(string Content, string GuildId, bool Status,
        List<EmbedFieldBuilder> Fields = null)
    {
        var dbGuild = await Guild.GetGuild(GuildId);
        var color = Status ? Color.Green : Color.Red;
        var embed = new EmbedBuilder()
            .WithDescription(Content)
            .WithColor(color)
            .WithCurrentTimestamp();
        if (Fields != null)
            foreach (var field in Fields.Where(f => f.Name != "null"))
                embed.AddField(field);

        var embedJson = JsonSerializer.Serialize(new List<EmbedBuilder> { embed },
            new JsonSerializerOptions { Converters = { new ColorJsonConverter() } });
        var dict = new Dictionary<string, string>
        {
            { "guildId", GuildId },
            { "channelId", dbGuild.LogChannel },
            { "message", "" },
            { "embedBuilders", embedJson },
            { "ping", "true" }
        };
        await new Queue("SendMessage", 2, dict, null).Insert();
    }
}