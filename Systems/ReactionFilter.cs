using System.Text.Json;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using DougBot.Models;
using DougBot.Systems;

namespace DougBot.Systems;

public static class ReactionFilter
{
    static Dictionary<ulong, List<string>> emoteWhitelists = new Dictionary<ulong, List<string>>();
    static List<Guild> dbGuilds = new List<Guild>();

    public static async Task Monitor(DiscordSocketClient client)
    {
        client.ReactionAdded += ReactionAddedHandler;
        Console.WriteLine("ReactionFilter Initialized");
        while (true)
        {
            try
            {
                dbGuilds = await Guild.GetGuilds();
                emoteWhitelists.Clear();
                foreach (var dbGuild in dbGuilds)
                {
                    //Get guild from client
                    var guild = client.Guilds.FirstOrDefault(g => g.Id.ToString() == dbGuild.Id);
                    //Compile whitelist
                    var guildEmotes = guild.Emotes;
                    var emoteWhitelist = guildEmotes.Select(e => e.Name).ToList();
                    emoteWhitelist.AddRange(dbGuild.ReactionFilterEmotes.Split(','));
                    emoteWhitelists.Add(guild.Id, emoteWhitelist);
                }
                await Task.Delay(600000);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        }
    }

    private static async Task ReactionAddedHandler(Cacheable<IUserMessage, ulong> Message,
        Cacheable<IMessageChannel, ulong> Channel, SocketReaction Reaction)
    {
        try
        {
            if (Channel.HasValue)
            {
                var channel = Channel.Value as SocketTextChannel;
                var guild = channel.Guild;
                var emote = Reaction.Emote;
                var user = Reaction.User.Value;
                var whitelist = emoteWhitelists[guild.Id];
                var dbGuild = dbGuilds.FirstOrDefault(g => g.Id == guild.Id.ToString());
                var whitelistChannels = dbGuild.ReactionFilterChannels.Split(",");
                if (whitelist != null && whitelistChannels != null)
                {
                    if (whitelistChannels.Contains(Channel.Id.ToString()) && !whitelist.Contains(emote.Name))
                    {
                        //Remove reaction
                        var reactDict = new Dictionary<string, string>
                        {
                            { "guildId", guild.Id.ToString() },
                            { "channelId", Reaction.Channel.Id.ToString() },
                            { "messageId", Reaction.MessageId.ToString() },
                            { "emoteName", Reaction.Emote.Name }
                        };
                        new Queue("RemoveReaction", 2, reactDict, null).Insert();
                        //Assign role and schedule removal
                        var roleDict = new Dictionary<string, string>
                        {
                            { "guildId", guild.Id.ToString() },
                            { "userId", user.Id.ToString() },
                            { "roleId", dbGuild.ReactionFilterRole }
                        };
                        new Queue("AddRole", null, roleDict, null).Insert();
                        var randomOffset = new Random().Next(1, 31);
                        new Queue("RemoveRole", null, roleDict, DateTime.UtcNow.AddDays(1).AddMinutes(randomOffset)).Insert();
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