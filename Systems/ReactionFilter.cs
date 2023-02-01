using System.Text.Json;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using DougBot.Models;

namespace DougBot.Systems;

public static class ReactionFilter
{
    public static async Task Filter(DiscordSocketClient client)
    {
        Console.WriteLine("Reaction Filter Initialized");
        while (true)
        {
            await Task.Delay(5000);
            try
            {
                //Get all guilds and loop them
                await using var db = new Database.DougBotContext();
                var dbGuilds = db.Guilds;
                foreach (var dbGuild in dbGuilds)
                {
                    //Get guild from client
                    var guild = client.Guilds.FirstOrDefault(g => g.Id.ToString() == dbGuild.Id);
                    //Compile whitelist
                    var guildEmotes = guild.Emotes;
                    var emoteWhitelist = guildEmotes.Select(e => e.Name).ToList();
                    emoteWhitelist.AddRange(dbGuild.ReactionFilterEmotes.Split(','));
                    //Get channels to filter and loop them
                    var channels = guild.Channels.Where(c => dbGuild.ReactionFilterChannels.Contains(c.Id.ToString()))
                        .ToList();
                    foreach (SocketTextChannel channel in channels)
                    {
                        //Get messages
                        var messages = await channel.GetMessagesAsync(5).FlattenAsync();
                        foreach (var message in messages)
                        {
                            //Declare log lists for audit post
                            var removedReactions = new Dictionary<string, string>();
                            //Loop reactions
                            var reactions = message.Reactions
                                .Where(r => !emoteWhitelist.Contains(r.Key.Name) &&
                                            r.Key.ToString() != "<::735492897608040489>");
                            foreach (var reaction in reactions)
                            {
                                //Get reaction users
                                var users = await message.GetReactionUsersAsync(reaction.Key, 500).FlattenAsync();
                                //Add to log
                                removedReactions.Add(reaction.Key.Name, string.Join(", ", users.Select(u => u.Mention)));
                                //Remove reaction
                                var reactDict = new Dictionary<string, string>
                                {
                                    { "guildId", guild.Id.ToString() },
                                    { "channelId", channel.Id.ToString() },
                                    { "messageId", message.Id.ToString() },
                                    { "emoteName", reaction.Key.Name }
                                };
                                var reactJson = JsonSerializer.Serialize(reactDict);
                                var queue = new Queue()
                                {
                                    Id = Guid.NewGuid().ToString(),
                                    Type = "RemoveReaction",
                                    Keys = reactJson,
                                    Priority = 2
                                };
                                await db.Queues.AddAsync(queue);
                                //Punish users
                                foreach (RestUser user in users)
                                {
                                    //Assign role and schedule removal
                                    var roleDict = new Dictionary<string, string>
                                    {
                                        { "guildId", guild.Id.ToString() },
                                        { "userId", user.Id.ToString() },
                                        { "roleId", dbGuild.ReactionFilterRole }
                                    };
                                    var roleJson = JsonSerializer.Serialize(roleDict);
                                    queue = new Queue()
                                    {
                                        Id = Guid.NewGuid().ToString(),
                                        Type = "AddRole",
                                        Keys = roleJson
                                    };
                                    await db.Queues.AddAsync(queue);
                                    queue = new Queue()
                                    {
                                        Id = Guid.NewGuid().ToString(),
                                        Type = "RemoveRole",
                                        Keys = roleJson,
                                        DueAt = DateTime.UtcNow.AddMinutes(30)
                                    };
                                    await db.Queues.AddAsync(queue);
                                }
                                db.SaveChangesAsync();
                            }
                            //Log to audit
                            if (removedReactions.Any())
                            {
                                var auditFields = new List<EmbedFieldBuilder>
                                {
                                    new()
                                    {
                                        Name = "Channel",
                                        Value = channel.Mention,
                                        IsInline = true
                                    },
                                    new()
                                    {
                                        Name = "Message",
                                        Value = message.Source,
                                        IsInline = true
                                    }
                                };
                                auditFields.AddRange(removedReactions.Select(reaction => new EmbedFieldBuilder() { Name = reaction.Key, Value = reaction.Value }));
                                AuditLog.LogEvent("***Reactions Removed***", dbGuild.Id, true, auditFields);
                            }
                        }
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