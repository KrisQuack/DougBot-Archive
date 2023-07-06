using Microsoft.EntityFrameworkCore;

namespace DougBot.Models;

public class Guild
{
    public string Id { get; set; } = null!;

    public string[]? ReactionFilterEmotes { get; set; }

    public string[]? ReactionFilterChannels { get; set; }

    public string? TwitchChannelName { get; set; }

    public string? TwitchChannelId { get; set; }

    public string? TwitchBotName { get; set; }

    public string? TwitchClientId { get; set; }

    public string? TwitchClientSecret { get; set; }

    public string? TwitchBotRefreshToken { get; set; }

    public string? TwitchChannelRefreshToken { get; set; }

    public string[]? TwitchContainsBlock { get; set; }

    public string[]? TwitchBlockedWords { get; set; }

    public string[]? TwitchEndsWithBlock { get; set; }

    public string? DmReceiptChannel { get; set; }

    public string? ReportChannel { get; set; }

    public string? OpenAiToken { get; set; }

    public string? OpenAiUrl { get; set; }

    public string? OpenAiChatForum { get; set; }

    public string? LogChannel { get; set; }

    public string[]? LogBlacklistChannels { get; set; }

    public virtual ICollection<Youtube> Youtubes { get; set; } = new List<Youtube>();

    public static async Task<Guild> GetGuild(string id)
    {
        await using var db = new DougBotContext();
        return await db.Guilds.Include(g => g.Youtubes).FirstOrDefaultAsync(g => g.Id == id);
    }

    public static async Task<List<Guild>> GetGuilds()
    {
        await using var db = new DougBotContext();
        return await db.Guilds.Include(g => g.Youtubes).ToListAsync();
    }

    public async Task Update()
    {
        await using var db = new DougBotContext();
        db.Guilds.Update(this);
        await db.SaveChangesAsync();
    }
}