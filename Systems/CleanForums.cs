using Discord;
using Discord.WebSocket;
using DougBot.Models;

namespace DougBot.Systems;

public static class CleanForums
{
    public static async Task Clean(DiscordSocketClient client)
    {
        Console.WriteLine("CleanForums Initialized");
        while (true)
        {
            await Task.Delay(3600000);
            try
            {
                var dbGuilds = await Guild.GetGuilds();
                foreach (var dbGuild in dbGuilds)
                {
                    //Get forums from client
                    var guild = client.Guilds.FirstOrDefault(g => g.Id.ToString() == dbGuild.Id);
                    var forums = guild.Channels.Where(c => c.GetType().Name == "SocketForumChannel");
                    //Loop all the forums in the guild
                    foreach (SocketForumChannel forum in forums)
                    {
                        //Get threads in the forum
                        var threads = await forum.GetActiveThreadsAsync();
                        var forumThreads = threads.Where(t => t.ParentChannelId == forum.Id);
                        //Loop threads
                        foreach (var thread in forumThreads)
                        {
                            //Check if the most recent message is older than 2 days and close if so
                            var message = await thread.GetMessagesAsync(1).FlattenAsync();
                            if (message.First().Timestamp.UtcDateTime < DateTime.UtcNow.AddDays(-2) ||
                                (!message.Any() && thread.CreatedAt.UtcDateTime < DateTime.UtcNow.AddDays(-2)))
                            {
                                await thread.ModifyAsync(t => t.Archived = true);
                                await AuditLog.LogEvent("**Thread Auto Closed**",dbGuild.Id, true, new List<EmbedFieldBuilder>
                                {
                                    new()
                                    {
                                        Name = "Forum",
                                        Value = forum.Mention,
                                        IsInline = true
                                    },
                                    new()
                                    {
                                        Name = "Age",
                                        Value = (DateTime.UtcNow - thread.CreatedAt.UtcDateTime).Days + " days",
                                        IsInline = true
                                    },
                                    new()
                                    {
                                        Name = "Messages",
                                        Value = thread.MessageCount.ToString(),
                                        IsInline = true
                                    },
                                    new()
                                    {
                                        Name = "Title",
                                        Value = $"[{thread.Name}](https://discord.com/channels/{thread.GuildId}/{thread.Id})",
                                        IsInline = false
                                    },
                                });
                            } 
                                
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}