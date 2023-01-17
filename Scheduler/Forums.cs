using Discord;
using Discord.WebSocket;
using DougBot.Models;

namespace DougBot.Scheduler;

public static class Forums
{
    public static async Task Clean(DiscordSocketClient client)
    {
        var settings = Setting.GetSettings();
        //Get forums from client
        var guild = client.Guilds.FirstOrDefault(g => g.Id.ToString() == settings.guildID);
        var forums = guild.Channels.Where(c => c.GetType().Name == "SocketForumChannel");
        //Loop the forums checking for posts inactive for 2 days or more
        foreach (SocketForumChannel forum in forums)
        {
            var threads = await forum.GetActiveThreadsAsync();
            var forumThreads = threads.Where(t => t.ParentChannelId == forum.Id);
            foreach (var thread in forumThreads)
            {
                var message = await thread.GetMessagesAsync(1).FlattenAsync();
                if (message.First().Timestamp.UtcDateTime < DateTime.UtcNow.AddDays(-2))
                    await thread.ModifyAsync(t => t.Archived = true);
                else if (!message.Any() && thread.CreatedAt.UtcDateTime < DateTime.UtcNow.AddDays(-2))
                    await thread.ModifyAsync(t => t.Archived = true);
            }
        }
    }
}