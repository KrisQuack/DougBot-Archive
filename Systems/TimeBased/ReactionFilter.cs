using Discord;

namespace DougBot.Systems.TimeBased;

public class ReactionFilter
{
    public static async Task Monitor()
    {
        Console.WriteLine("Reaction Filter Initialized");
        while (true)
        {
            await Task.Delay(300000);
            try
            {
                //Get data
                var messageCount = 20;
                using var httpClient = new HttpClient();
                //Get guild and filter list
                var guild = ConfigurationService.Instance.Guild;
                var guildUsers = await guild.GetUsersAsync().FlattenAsync();
                var guildMods = guildUsers.Where(u => u.GuildPermissions.ModerateMembers);
                var guildEmotes = guild.Emotes;
                var emoteWhitelist = guildEmotes.Select(e => e.Name).ToList();
                emoteWhitelist.AddRange(ConfigurationService.Instance.ReactionFilterEmotes);
                //Loop through channels
                var channels = guild.TextChannels.Where(ConfigurationService.Instance.ReactionFilterChannels.Contains);
                foreach (var channel in channels)
                {
                    var messages = await channel.GetMessagesAsync(messageCount).FlattenAsync();
                    foreach (var message in messages)
                    {
                        //Get reactions to remove
                        var reactions = message.Reactions.Where(r => !emoteWhitelist.Contains(r.Key.Name));
                        //Remove reactions
                        foreach (var reaction in reactions)
                        {
                            //get users who reacted
                            var reactionUsers = await message.GetReactionUsersAsync(reaction.Key, messageCount).FlattenAsync();
                            //hcek if any users are mods using IDs
                            if(reactionUsers.Any(u => guildMods.Any(m => m.Id == u.Id))) continue;
                            //Remove reactions
                            await message.RemoveAllReactionsForEmoteAsync(reaction.Key);
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[General/Warning] {DateTime.UtcNow:HH:mm:ss} ReactionFilter {ex}");
            }
        }
    }
}