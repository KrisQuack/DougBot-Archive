using Discord;
using Discord.WebSocket;
using DougBot.Models;

namespace DougBot.Systems;

public static class ReactionFilter
{
    private static readonly Dictionary<ulong, List<string>> emoteWhitelists = new();
    private static List<Guild> dbGuilds = new();

    public static async Task Monitor(DiscordSocketClient client)
    {
        client.ReactionAdded += ReactionAddedHandler;
        Console.WriteLine("ReactionFilter Initialized");
        while (true)
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

    private static Task ReactionAddedHandler(Cacheable<IUserMessage, ulong> Message,
        Cacheable<IMessageChannel, ulong> Channel, SocketReaction Reaction)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                //Skip if no guilds or emotes
                if (!dbGuilds.Any() && !emoteWhitelists.Any())
                    return;
                //Load values
                var guild = (Reaction.Channel as SocketTextChannel).Guild;
                var emote = Reaction.Emote;
                var whitelist = emoteWhitelists[guild.Id];
                var dbGuild = dbGuilds.FirstOrDefault(g => g.Id == guild.Id.ToString());
                var whitelistChannels = dbGuild.ReactionFilterChannels.Split(",");
                //Check if emote is whitelisted
                if (whitelist != null && whitelistChannels != null)
                    if (whitelistChannels.Contains(Reaction.Channel.Id.ToString()) && !whitelist.Contains(emote.Name))
                    {
                        //Remove reaction
                        var reactDict = new Dictionary<string, string>
                        {
                            { "guildId", guild.Id.ToString() },
                            { "channelId", Reaction.Channel.Id.ToString() },
                            { "messageId", Reaction.MessageId.ToString() },
                            { "emoteName", Reaction.Emote.Name }
                        };
                        //Get message if not cached
                        IMessage realMessage;
                        if (!Message.HasValue)
                            realMessage = await Channel.Value.GetMessageAsync(Reaction.MessageId);
                        else
                            realMessage = Message.Value;
                        //Queue removal
                        var dueTime = realMessage.Timestamp.AddMinutes(1).DateTime;
                        await new Queue("RemoveReaction", 3, reactDict, dueTime).Insert();
                    }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
        });
        return Task.CompletedTask;
    }
}