using System.Text.Json;
using Discord;
using DougBot.Models;

namespace DougBot.Systems;

public static class AuditLog
{
    public static async Task LogEvent(string content, string guildId, bool status,
        List<EmbedFieldBuilder> fields = null)
    {
        await using var db = new Database.DougBotContext();
        var dbGuild = await db.Guilds.FindAsync(guildId);

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
            { "guildId", guildId },
            { "channelId", dbGuild.LogChannel },
            { "message", "" },
            { "embedBuilders", embedJson },
            { "ping", "true" }
        };
        var json = JsonSerializer.Serialize(dict);
        var queue = new Queue()
        {
            Id = Guid.NewGuid().ToString(),
            Type = "SendMessage",
            Keys = json
        };
        await db.Queues.AddAsync(queue);
        await db.SaveChangesAsync();
    }
}