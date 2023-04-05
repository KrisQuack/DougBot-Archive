using Microsoft.EntityFrameworkCore;

namespace DougBot.Models;

public class Guild
{
    public string? Id { get; set; }
    public string? ReactionFilterEmotes { get; set; }
    public string? ReactionFilterChannels { get; set; }
    public string? ReactionFilterRole { get; set; }
    public List<YoutubeSetting>? YoutubeSettings { get; set; }
    public string? DmReceiptChannel { get; set; }
    public string? OpenAiToken { get; set; }
    public string? OpenAiURL { get; set; }
    public string? LogChannel { get; set; }
    public string? LogBlacklistChannels { get; set; }
    public TwitchSetting TwitchSettings { get; set; }

    public static async Task<Guild> GetGuild(string id)
    {
        await using var db = new Database.DougBotContext();
        return await db.Guilds.FindAsync(id);
    }

    public static async Task<List<Guild>> GetGuilds()
    {
        await using var db = new Database.DougBotContext();
        return await db.Guilds.ToListAsync();
    }

    public async Task Update()
    {
        await using var db = new Database.DougBotContext();
        db.Guilds.Update(this);
        await db.SaveChangesAsync();
    }
}