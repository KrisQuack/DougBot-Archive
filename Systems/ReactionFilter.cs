using Discord;
using Discord.WebSocket;
using DougBot.Models;
using DougBot.Scheduler;

namespace DougBot.Systems;

public static class ReactionFilter
{
    private static readonly Dictionary<ulong, List<string>> EmoteWhitelists = new();
    private static List<Guild> _dbGuilds = new();

    public static async Task Monitor()
    {
        var client = Program.Client;
        client.ReactionAdded += ReactionAddedHandler;
        Console.WriteLine("ReactionFilter Initialized");
        while (true)
            try
            {
                _dbGuilds = await Guild.GetGuilds();
                EmoteWhitelists.Clear();
                foreach (var dbGuild in _dbGuilds)
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
                    EmoteWhitelists.Add(guild.Id, emoteWhitelist);
                }

                await Task.Delay(600000);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.ToString());
            }
    }

    private static Task ReactionAddedHandler(Cacheable<IUserMessage, ulong> message,
        Cacheable<IMessageChannel, ulong> channel, SocketReaction reaction)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                var guild = (reaction.Channel as SocketTextChannel).Guild;
                //Skip if emoteWhitelists does not contain guild
                if (!EmoteWhitelists.ContainsKey(guild.Id) || !_dbGuilds.Any())
                    return;
                //Load values
                var emote = reaction.Emote;
                var whitelist = EmoteWhitelists[guild.Id];
                var dbGuild = _dbGuilds.FirstOrDefault(g => g.Id == guild.Id.ToString());
                var whitelistChannels = dbGuild.ReactionFilterChannels.Split(",");
                //Check if emote is whitelisted
                if (whitelist != null && whitelistChannels != null)
                    if (whitelistChannels.Contains(reaction.Channel.Id.ToString()) && !whitelist.Contains(emote.Name))
                    {
                        //Get message
                        IMessage realMessage;
                        if (!message.HasValue)
                            realMessage = await channel.Value.GetMessageAsync(reaction.MessageId);
                        else
                            realMessage = message.Value;
                        //Set target time
                        var targetTime = realMessage.Timestamp.AddMinutes(1).UtcDateTime;
                        var minTargetTime = DateTime.UtcNow.AddSeconds(10);
                        if (targetTime < minTargetTime) targetTime = minTargetTime;
                        await RemoveReactionJob.Queue(guild.Id.ToString(), reaction.Channel.Id.ToString(),
                            reaction.MessageId.ToString(), emote.Name, targetTime);
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