using System.Text.Json;
using Discord;
using DougBot.Models;

namespace DougBot.Systems;

public static class AuditLog
{
    public static async Task LogEvent(string content, bool status,
        List<EmbedFieldBuilder> fields = null)
    {
        var settings = Setting.GetSettings();
        var color = status ? Color.Green : Color.Red;
        var embed = new EmbedBuilder()
            .WithDescription(content)
            .WithColor(color)
            .WithCurrentTimestamp();
        if (fields != null)
            foreach (var field in fields.Where(f => f.Name != "null"))
                embed.AddField(field);

        var embedJson = JsonSerializer.Serialize(new List<EmbedBuilder> { embed },
            new JsonSerializerOptions { Converters = { new ColorJsonConverter() } });
        var dict = new Dictionary<string, string>
        {
            { "guildId", settings.guildID },
            { "channelId", settings.logChannel },
            { "message", "" },
            { "embedBuilders", embedJson },
            { "ping", "true" }
        };
        var json = JsonSerializer.Serialize(dict);
        Queue.Create("SendMessage", null, json, DateTime.UtcNow);
    }
}