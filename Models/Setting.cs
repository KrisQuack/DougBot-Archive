namespace DougBot.Models;

public class Setting
{
    public int Id { get; set; }
    public string Token { get; set; }
    public string guildID { get; set; }
    public string reactionFilterEmotes { get; set; }
    public string reactionFilterChannels { get; set; }
    public string reactionFilterRole { get; set; }
    public string YoutubePostChannel { get; set; }
    public string YoutubeChannels { get; set; }
    public DateTime YoutubeLastCheck { get; set; }
    public string OpenAiToken { get; set; }
    public string dmReceiptChannel { get; set; }

    public static Setting GetSettings()
    {
        using var db = new Database.DougBotContext();
        return db.Settings.FirstOrDefault();
    }

    public static async Task UpdateReactionFilter(string reactionFilterEmotes, string reactionFilterChannels,
        string reactionFilterRole)
    {
        await using var db = new Database.DougBotContext();
        var settings = db.Settings.FirstOrDefault();
        settings.reactionFilterEmotes = reactionFilterEmotes;
        settings.reactionFilterChannels = reactionFilterChannels;
        settings.reactionFilterRole = reactionFilterRole;
        await db.SaveChangesAsync();
    }

    public static async Task UpdateYoutubeFeed(string YoutubePostChannel, string YoutubeChannels)
    {
        await using var db = new Database.DougBotContext();
        var settings = db.Settings.FirstOrDefault();
        settings.YoutubePostChannel = YoutubePostChannel;
        settings.YoutubeChannels = YoutubeChannels;
        await db.SaveChangesAsync();
    }

    public static async Task UpdateLastChecked(DateTime lastChecked)
    {
        await using var db = new Database.DougBotContext();
        var settings = db.Settings.FirstOrDefault();
        settings.YoutubeLastCheck = lastChecked;
        await db.SaveChangesAsync();
    }

    public static async Task UpdateAdmin(string modalOpenAiToken, string modalDmReceiptChannel)
    {
        await using var db = new Database.DougBotContext();
        var settings = db.Settings.FirstOrDefault();
        settings.OpenAiToken = modalOpenAiToken;
        settings.dmReceiptChannel = modalDmReceiptChannel;
        await db.SaveChangesAsync();
    }
}