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
    public string? OpenAiWordBlacklist { get; set; }
    public string? LogChannel { get; set; }
}