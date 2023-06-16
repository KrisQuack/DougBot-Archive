using System.Security.Cryptography;
using System.Text;
using Discord;
using Discord.WebSocket;
using Quartz;

namespace DougBot.Scheduler;

public class AddReactionJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var dataMap = context.JobDetail.JobDataMap;
            var client = Program.Client;
            var guildId = Convert.ToUInt64(dataMap.GetString("guildId"));
            var channelId = Convert.ToUInt64(dataMap.GetString("channelId"));
            var messageId = Convert.ToUInt64(dataMap.GetString("messageId"));
            var emoteName = dataMap.GetString("emoteName");

            
            var guild = client.Guilds.FirstOrDefault(g => g.Id == guildId);
            var channel = guild.Channels.FirstOrDefault(c => c.Id == channelId) as SocketTextChannel;
            var message = await channel.GetMessageAsync(messageId);
            await message.AddReactionAsync(new Emoji(emoteName));
        }
        catch (Exception e)
        {
            Console.WriteLine($"[General/Warning] {DateTime.UtcNow:HH:mm:ss} RemoveReactionJob {e}");
        }
    }

    public static async Task Queue(string guildId, string channelId, string messageId, string emoteName, DateTime schedule)
    {
        try
        {
            //Create a Sha1 hash from the message id and emote name
            var hash = SHA1.HashData(Encoding.UTF8.GetBytes($"{messageId}{emoteName}"));
            var hashString = BitConverter.ToString(hash).Replace("-", "").ToLower();
            //Check if trigger already exists
            var trigger =
                await Quartz.MemorySchedulerInstance.GetTrigger(
                    new TriggerKey($"addReactionJob-{hashString}", guildId));
            if (trigger != null)
                return;
            //If not, create a new trigger
            var addReactionJob = JobBuilder.Create<RemoveReactionJob>()
                .WithIdentity($"addReactionJob-{hashString}", guildId)
                .UsingJobData("guildId", guildId)
                .UsingJobData("channelId", channelId)
                .UsingJobData("messageId", messageId)
                .UsingJobData("emoteName", emoteName)
                .Build();
            var addReactionTrigger = TriggerBuilder.Create()
                .WithIdentity($"addReactionJob-{hashString}", guildId)
                .StartAt(schedule)
                .Build();
            if (schedule > DateTime.UtcNow.AddMinutes(10))
                await Quartz.PersistentSchedulerInstance.ScheduleJob(addReactionJob, addReactionTrigger);
            else
                await Quartz.MemorySchedulerInstance.ScheduleJob(addReactionJob, addReactionTrigger);
        }
        catch (Exception e)
        {
            Console.WriteLine($"[General/Warning] {DateTime.UtcNow:HH:mm:ss} RemoveReactionQueue {e}");
        }
    }
}