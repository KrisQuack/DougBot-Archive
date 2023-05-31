using Quartz;

namespace DougBot.Scheduler;

public class DeleteMessageJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var dataMap = context.JobDetail.JobDataMap;
            var guildId = Convert.ToUInt64(dataMap.GetString("guildId"));
            var channelId = Convert.ToUInt64(dataMap.GetString("channelId"));
            var messageId = Convert.ToUInt64(dataMap.GetString("messageId"));

            var client = Program._Client;
            var guild = client.GetGuild(guildId);
            var channel = guild.GetTextChannel(channelId);
            await channel.DeleteMessageAsync(messageId);
        }
        catch (Exception e)
        {
            Console.WriteLine($"[General/Warning] {DateTime.UtcNow:HH:mm:ss} DeleteMessageJob {e}");
        }
    }

    public static async Task Queue(string guildId, string channelId, string messageId, DateTime schedule)
    {
        try
        {
            var deleteMessageJob = JobBuilder.Create<DeleteMessageJob>()
                .WithIdentity($"deleteMessageJob-{Guid.NewGuid()}", guildId)
                .UsingJobData("guildId", guildId)
                .UsingJobData("channelId", channelId)
                .UsingJobData("messageId", messageId)
                .Build();
            var deleteMessageTrigger = TriggerBuilder.Create()
                .WithIdentity($"deleteMessageTrigger-{Guid.NewGuid()}", guildId)
                .StartAt(schedule)
                .Build();
            await Quartz.MemorySchedulerInstance.ScheduleJob(deleteMessageJob, deleteMessageTrigger);
        }
        catch (Exception e)
        {
            Console.WriteLine($"[General/Warning] {DateTime.UtcNow:HH:mm:ss} DeleteMessageQueue {e}");
        }
    }
}