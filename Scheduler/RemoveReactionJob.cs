using Discord.WebSocket;
using Quartz;

namespace DougBot.Scheduler;

public class RemoveReactionJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var dataMap = context.JobDetail.JobDataMap;
        var client = Program._Client;
        var guildId = Convert.ToUInt64(dataMap.GetString("guildId"));
        var channelId = Convert.ToUInt64(dataMap.GetString("channelId"));
        var messageId = Convert.ToUInt64(dataMap.GetString("messageId"));
        var emoteName = dataMap.GetString("emoteName");

        var guild = client.Guilds.FirstOrDefault(g => g.Id == guildId);
        var channel = guild.Channels.FirstOrDefault(c => c.Id == channelId) as SocketTextChannel;
        var message = await channel.GetMessageAsync(messageId);
        var emote = message.Reactions.FirstOrDefault(r => r.Key.Name == emoteName).Key;
        await message.RemoveAllReactionsForEmoteAsync(emote);
    }
}