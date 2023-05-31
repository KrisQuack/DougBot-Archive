using Discord;
using Discord.WebSocket;
using DougBot.Models;
using DougBot.Scheduler;

namespace DougBot.Systems;

public static class ReactionFilter
{
    private static readonly Dictionary<ulong, List<string>> emoteWhitelists = new();
    private static List<Guild> dbGuilds = new();

    public static async Task Monitor()
    {
        var client = Program._Client;
        client.ReactionAdded += ReactionAddedHandler;
        Console.WriteLine("ReactionFilter Initialized");
        while (true)
            try
            {
                dbGuilds = await Guild.GetGuilds();
                emoteWhitelists.Clear();
                foreach (var dbGuild in dbGuilds)
                {
                    //If settings are null, skip
                    if (string.IsNullOrEmpty(dbGuild.ReactionFilterEmotes))
                        continue;
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
                var guild = (Reaction.Channel as SocketTextChannel).Guild;
                //Skip if emoteWhitelists does not contain guild
                if (!emoteWhitelists.ContainsKey(guild.Id) || !dbGuilds.Any())
                    return;
                //Load values
                var emote = Reaction.Emote;
                var whitelist = emoteWhitelists[guild.Id];
                var dbGuild = dbGuilds.FirstOrDefault(g => g.Id == guild.Id.ToString());
                var whitelistChannels = dbGuild.ReactionFilterChannels.Split(",");
                //Check if emote is whitelisted
                if (whitelist != null && whitelistChannels != null)
                    if (whitelistChannels.Contains(Reaction.Channel.Id.ToString()) && !whitelist.Contains(emote.Name))
                    {
                        //Get message
                        IMessage realMessage;
                        if (!Message.HasValue)
                            realMessage = await Channel.Value.GetMessageAsync(Reaction.MessageId);
                        else
                            realMessage = Message.Value;
                        //Set target time
                        var targetTime = realMessage.Timestamp.AddMinutes(1).UtcDateTime;
                        var minTargetTime = DateTime.UtcNow.AddSeconds(10);
                        if (targetTime < minTargetTime) targetTime = minTargetTime;
                        await RemoveReactionJob.Queue(guild.Id.ToString(), Reaction.Channel.Id.ToString(),
                            Reaction.MessageId.ToString(), emote.Name, targetTime);
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