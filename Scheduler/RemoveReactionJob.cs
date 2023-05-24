using Discord.WebSocket;
using Quartz;

namespace DougBot.Scheduler;

public class RemoveReactionJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var dataMap = context.JobDetail.JobDataMap;
            var client = Program._Client;
            var guildId = Convert.ToUInt64(dataMap.GetString("guildId"));
            var channelId = Convert.ToUInt64(dataMap.GetString("channelId"));
            var messageId = Convert.ToUInt64(dataMap.GetString("messageId"));
            var emoteName = dataMap.GetString("emoteName");

            //check for nulls and return if any are null
            if (guildId == 0 || channelId == 0 || messageId == 0 || emoteName == null)
                return;

            var guild = client.Guilds.FirstOrDefault(g => g.Id == guildId);
            var channel = guild.Channels.FirstOrDefault(c => c.Id == channelId) as SocketTextChannel;
            var message = await channel.GetMessageAsync(messageId);
            var emote = message.Reactions.FirstOrDefault(r => r.Key.Name == emoteName).Key;
            //check for nulls and return if any are null
            if (emote == null)
                return;
            await message.RemoveAllReactionsForEmoteAsync(emote);
        }
        catch (Exception e)
        {
            Console.WriteLine($"[General/Warning] {DateTime.UtcNow:HH:mm:ss} RemoveReactionJob {e}");
        }
    }
}