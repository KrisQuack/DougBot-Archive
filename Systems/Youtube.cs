using System.Text.Json;
using Discord;
using DougBot.Models;
using YoutubeExplode;
using YoutubeExplode.Common;

namespace DougBot.Scheduler;

public static class Youtube
{
    public static async Task CheckYoutube()
    {
        Console.WriteLine("Youtube Initialized");
        while (true)
        {
            await Task.Delay(300000);
            try
            {
                //Set up youtube client
                var youtube = new YoutubeClient();
                //Get all guilds and loop them
                await using var db = new Database.DougBotContext();
                var dbGuilds = db.Guilds;
                foreach (var dbGuild in dbGuilds)
                {
                    foreach (var dbYoutube in dbGuild.YoutubeSettings)
                    {
                        //Get youtube details
                        var ytChannel = await youtube.Channels.GetAsync(dbYoutube.ChannelId);
                        var uploads = await youtube.Channels.GetUploadsAsync(dbYoutube.ChannelId);
                        var lastUpload = uploads.FirstOrDefault();
                        var video = await youtube.Videos.GetAsync(lastUpload.Id);
                        //Check if video was pinged before or if the bot was just started
                        if (video.Id.ToString() != dbYoutube.LastVideoId)
                        {
                            //Build the ping embed
                            var embed = new EmbedBuilder()
                                .WithAuthor(ytChannel.Title, ytChannel.Thumbnails[0].Url, ytChannel.Url)
                                .WithTitle(video.Title)
                                .WithImageUrl(video.Thumbnails.OrderBy(t => t.Resolution.Area).Last().Url)
                                .WithUrl(video.Url);
                            var embedJson = JsonSerializer.Serialize(new List<EmbedBuilder> { embed },
                                new JsonSerializerOptions { Converters = { new ColorJsonConverter() } });
                            //Add to queue
                            var dict = new Dictionary<string, string>
                            {
                                { "guildId", dbGuild.Id },
                                { "channelId", dbYoutube.PostChannel },
                                { "message", $"<@&{dbYoutube.MentionRole}>" },
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
                        }
                        dbYoutube.LastVideoId = video.Id;
                        db.SaveChangesAsync();
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
        }
    }
}