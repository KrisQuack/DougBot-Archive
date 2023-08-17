using System.Xml.Linq;
using Discord;

namespace DougBot.Systems.TimeBased;

public class CheckYoutube
{
    private static readonly HttpClient httpClient = new();
    private static readonly XNamespace atom = "http://www.w3.org/2005/Atom";
    private static readonly XNamespace media = "http://search.yahoo.com/mrss/";
    private static readonly XNamespace yt = "http://www.youtube.com/xml/schemas/2015";

    public static async Task Monitor()
    {
        Console.WriteLine("Youtube Initialized");
        while (true)
        {
            await Task.Delay(600000);
            try
            {
                foreach (var Youtube in ConfigurationService.Instance.YoutubeConfigs)
                    try
                    {
                        var ytFeed =
                            await httpClient.GetStringAsync("https://www.youtube.com/feeds/videos.xml?channel_id=" +
                                                            Youtube.Id);
                        var xDoc = XDocument.Parse(ytFeed);

                        var channel = xDoc.Descendants(atom + "author").First();
                        var channelName = channel.Element(atom + "name").Value;
                        var channelUrl = channel.Element(atom + "uri").Value;
                        var latestVideo = xDoc.Descendants(atom + "entry")
                            .OrderByDescending(e => DateTime.Parse(e.Element(atom + "published").Value))
                            .First();
                        var videoTitle = latestVideo.Element(atom + "title").Value;
                        var videoThumbnail =
                            latestVideo.Descendants(media + "thumbnail").First().Attribute("url").Value;
                        var videoId = latestVideo.Element(yt + "videoId").Value;
                        var videoUrl = latestVideo.Element(atom + "link").Attribute("href").Value;

                        if (string.IsNullOrEmpty(Youtube.LastVideoId)) Youtube.LastVideoId = videoId;
                        if (videoId == Youtube.LastVideoId) continue;

                        var mentionRole = "";
                        if (Youtube.Id == "UCzL0SBEypNk4slpzSbxo01g" && !videoTitle.Contains("VOD"))
                            mentionRole = "<@&812501073289805884>";
                        else mentionRole = $"<@&{Youtube.MentionRole}>";

                        var embed = new EmbedBuilder()
                            .WithAuthor(channelName, "", channelUrl)
                            .WithTitle(videoTitle)
                            .WithImageUrl(videoThumbnail)
                            .WithUrl(videoUrl);
                        await ConfigurationService.Instance.Guild
                            .GetTextChannel(Convert.ToUInt64(Youtube.PostChannel))
                            .SendMessageAsync(mentionRole, false, embed.Build(), allowedMentions: AllowedMentions.All);
                        Youtube.LastVideoId = videoId;
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            $"[General/Warning] {DateTime.UtcNow:HH:mm:ss} YoutubeChannel {Youtube.Id} {ex}");
                    }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[General/Warning] {DateTime.UtcNow:HH:mm:ss} CheckYoutubeJob {ex}");
            }
        }
    }
}