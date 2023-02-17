using System.Text.Json;
using Discord;
using Discord.Rest;
using Discord.WebSocket;
using DougBot.Models;
using DougBot.Systems;

namespace DougBot.Systems;

public static class ReactionFilter
{
    static Dictionary<ulong, List<string>> emoteWhitelists = new();
    static List<Guild> dbGuilds = new();

    public static async Task Monitor(DiscordSocketClient client)
    {
        client.ReactionAdded += ReactionAddedHandler;
        client.ReactionAdded += async (message, channel, reaction) => Task.Run(() => ReactionAddedHandler(message, channel, reaction));
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
            if (!Channel.HasValue || !dbGuilds.Any())
                return;
            var channel = Channel.Value as SocketTextChannel;
            var guild = channel.Guild;
            var emote = Reaction.Emote;
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
                    var dueTime = Message.Value.Timestamp.AddMinutes(1).DateTime;
                    await new Queue("RemoveReaction", 3, reactDict, dueTime).Insert();
                }
            }
        }
        catch (Exception ex)
        {
            Console.WriteLine(ex.ToString());
        }
    }
}