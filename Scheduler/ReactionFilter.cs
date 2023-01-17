using Discord;
using Discord.Rest;
using Discord.WebSocket;
using DougBot.Models;
using System.Text.Json;

namespace DougBot.Scheduler;

public static class ReactionFilter
{
    public static async Task Filter(DiscordSocketClient client, int messageCount)
    {
        try
        {
            var settings = Setting.GetSettings();
            var guild = client.Guilds.FirstOrDefault(g => g.Id.ToString() == settings.guildID);
            var guildEmotes = guild.Emotes;
            var emoteWhitelist = guildEmotes.Select(e => e.Name).ToList();
            emoteWhitelist.AddRange(settings.reactionFilterEmotes.Split(','));
            var channels = guild.Channels.Where(c => settings.reactionFilterChannels.Contains(c.Id.ToString()))
                .ToList();
            foreach (SocketTextChannel channel in channels)
            {
                //Get messages
                var messages = await channel.GetMessagesAsync(messageCount).FlattenAsync();
                foreach (var message in messages)
                {
                    //Loop reactions
                    var reactions = message.Reactions
                        .Where(r => !emoteWhitelist.Contains(r.Key.Name) && r.Key.Name != null)
                        .DistinctBy(r => r.Key.Name);
                    foreach (var reaction in reactions)
                    {
                        //Get reaction users
                        var users = await message.GetReactionUsersAsync(reaction.Key, 500).FlattenAsync();
                        //Remove reaction
                        var reactDict = new Dictionary<string, string>
                        {
                            { "guildId", guild.Id.ToString() },
                            { "channelId", channel.Id.ToString() },
                            { "messageId", message.Id.ToString() },
                            { "emoteName", reaction.Key.Name }
                        };
                        var reactJson = JsonSerializer.Serialize(reactDict);
                        Queue.Create("RemoveReaction", null, reactJson, DateTime.UtcNow);
                        //Punish users
                        foreach (RestUser user in users)
                        {
                            //Assign role and schedule removal
                            var roleDict = new Dictionary<string, string>
                            {
                                { "guildId", guild.Id.ToString() },
                                { "userId", user.Id.ToString() },
                                { "roleId", settings.reactionFilterRole }
                            };
                            var roleJson = JsonSerializer.Serialize(roleDict);
                            Queue.Create("AddRole", null, roleJson, DateTime.UtcNow);
                            Queue.Create("RemoveRole", null, roleJson, DateTime.UtcNow.AddMinutes(10));
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