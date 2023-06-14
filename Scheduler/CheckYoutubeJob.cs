using System.Xml.Linq;
using Discord;
using DougBot.Models;
using Quartz;

namespace DougBot.Scheduler;

public class CheckYoutubeJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var dbGuilds = await Guild.GetGuilds();
            using var httpClient = new HttpClient();
            foreach (var dbGuild in dbGuilds)
            {
                if (dbGuild.YoutubeSettings == null) continue;
                foreach (var dbYoutube in dbGuild.YoutubeSettings)
                    try
                    {
                        var ytFeed =
                            await httpClient.GetStringAsync("https://www.youtube.com/feeds/videos.xml?channel_id=" +
                                                            dbYoutube.Id);
                        var xDoc = XDocument.Parse(ytFeed);
                        XNamespace atom = "http://www.w3.org/2005/Atom";
                        XNamespace media = "http://search.yahoo.com/mrss/";
                        XNamespace yt = "http://www.youtube.com/xml/schemas/2015";

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

                        if (videoId == dbYoutube.LastVideoId) continue;

                        var mentionRole = "";
                        if (dbYoutube.Id == "UCzL0SBEypNk4slpzSbxo01g" && !videoTitle.Contains("VOD"))
                            mentionRole = "<@&812501073289805884>";
                        else mentionRole = $"<@&{dbYoutube.MentionRole}>";

                        var embed = new EmbedBuilder()
                            .WithAuthor(channelName, "", channelUrl)
                            .WithTitle(videoTitle)
                            .WithImageUrl(videoThumbnail)
                            .WithUrl(videoUrl);
                        await SendMessageJob.Queue(dbGuild.Id, dbYoutube.PostChannel, new List<EmbedBuilder> { embed },
                            DateTime.UtcNow, mentionRole, true);
                        dbYoutube.LastVideoId = videoId;
                        await dbGuild.Update();
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(
                            $"[General/Warning] {DateTime.UtcNow:HH:mm:ss} YoutubeChannel {dbYoutube.Id} {ex}");
                    }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[General/Warning] {DateTime.UtcNow:HH:mm:ss} CheckYoutubeJob {ex}");
        }
    }
}