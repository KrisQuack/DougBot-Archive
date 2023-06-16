using System;
using System.Xml.Linq;
using Discord;
using DougBot.Models;
using Quartz;

namespace DougBot.Scheduler;

public class ReactionFilterJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            //Get data
            var dataMap = context.JobDetail.JobDataMap;
            var messageCount =dataMap.GetInt("messageCount");
            var dbGuilds = await Guild.GetGuilds();
            using var httpClient = new HttpClient();
            foreach (var dbGuild in dbGuilds)
            {
                //Get guild and filter list
                if (dbGuild.ReactionFilterChannels == null || dbGuild.ReactionFilterEmotes == null) continue;
                var guild = Program.Client.GetGuild(ulong.Parse(dbGuild.Id));
                var guildEmotes = guild.Emotes;
                var emoteWhitelist = guildEmotes.Select(e => e.Name).ToList();
                emoteWhitelist.AddRange(dbGuild.ReactionFilterEmotes);
                //Loop through channels
                var channels = guild.TextChannels.Where(c => dbGuild.ReactionFilterChannels.Contains(c.Id.ToString()));
                foreach (var channel in channels)
                {
                    var messages = await channel.GetMessagesAsync(messageCount).FlattenAsync();
                    foreach (var message in messages)
                    {
                        //Get reactions to remove
                        var reactions = message.Reactions.Where(r => !emoteWhitelist.Contains(r.Key.Name));
                        //Remove reactions
                        foreach (var reaction in reactions)
                        {
                            await message.RemoveAllReactionsForEmoteAsync(reaction.Key);
                        }
                    }
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine($"[General/Warning] {DateTime.UtcNow:HH:mm:ss} ReactionFilter {ex}");
        }
    }
}