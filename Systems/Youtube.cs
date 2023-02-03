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
                var dbGuilds = await Guild.GetGuilds();
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
                            //Set mention role, special block to allow VOD filtering for DougDogDougDog
                            var mentionRole = dbYoutube.MentionRole;
                            if (dbYoutube.ChannelId == "UCzL0SBEypNk4slpzSbxo01g" && !video.Title.Contains("VOD"))
                            {
                                mentionRole = "812501073289805884";
                            }
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
                                { "message", $"<@&{mentionRole}>" },
                                { "embedBuilders", embedJson },
                                { "ping", "true" }
                            };
                            await new Queue("SendMessage", null, dict, null).Insert();
                        }
                        dbYoutube.LastVideoId = video.Id;
                        await dbGuild.Update();
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