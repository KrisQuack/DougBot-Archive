namespace DougBot.Models;

public class TwitchSetting
{
    public string? ChannelName { get; set; }
    public string? ChannelId { get; set; }
    public string? BotName { get; set; }
    public string? ClientId { get; set; }
    public string? ClientSecret { get; set; }
    public string? BotRefreshToken { get; set; }
    public string? ChannelRefreshToken { get; set; }
    public string[] ContainsBlock { get; set; }
    public string[] BlockedWords { get; set; }
    public string[] EndsWithBlock { get; set; }
}