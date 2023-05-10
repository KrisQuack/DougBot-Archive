using Discord;
using Discord.WebSocket;
using DougBot.Models;
using Quartz;

namespace DougBot.Scheduler;

public class CleanForumsJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var dbGuilds = await Guild.GetGuilds();
        var client = Program._Client;

        foreach (var dbGuild in dbGuilds)
        {
            var guild = client.Guilds.FirstOrDefault(g => g.Id.ToString() == dbGuild.Id);
            var forums = guild.Channels.Where(c => c.GetType().Name == "SocketForumChannel");

            foreach (var socketGuildChannel in forums)
            {
                var forum = (SocketForumChannel)socketGuildChannel;
                var threads = await forum.GetActiveThreadsAsync();
                var forumThreads = threads.Where(t => t.ParentChannelId == forum.Id);

                foreach (var thread in forumThreads)
                {
                    var message = await thread.GetMessagesAsync(1).FlattenAsync();
                    //if the thread has no messages or the last message is older than 2 days, archive the thread
                    if ((message.Any() && message.First().Timestamp.UtcDateTime < DateTime.UtcNow.AddDays(-2)) ||
                        (!message.Any() && thread.CreatedAt.UtcDateTime < DateTime.UtcNow.AddDays(-2)))
                        await thread.ModifyAsync(t => t.Archived = true);
                }
            }
        }
    }
}