using Discord.WebSocket;

namespace DougBot.Scheduler;

public static class Reaction
{
    public static async Task Remove(DiscordSocketClient client, ulong guidID, ulong channelID, ulong messageID,
        string emoteName)
    {
        var guild = client.Guilds.FirstOrDefault(g => g.Id == guidID);
        var channel = guild.Channels.FirstOrDefault(c => c.Id == channelID) as SocketTextChannel;
        var message = await channel.GetMessageAsync(messageID);
        var emote = message.Reactions.FirstOrDefault(r => r.Key.Name == emoteName).Key;
        message.RemoveAllReactionsForEmoteAsync(emote);
    }
}