using Quartz;

namespace DougBot.Scheduler;

public class RemoveRoleJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var dataMap = context.JobDetail.JobDataMap;
            var client = Program.Client;
            var guildId = Convert.ToUInt64(dataMap.GetString("guildId"));
            var userId = Convert.ToUInt64(dataMap.GetString("userId"));
            var roleId = Convert.ToUInt64(dataMap.GetString("roleId"));

            //check for nulls and return if any are null
            if (guildId == 0 || userId == 0 || roleId == 0)
                return;

            var guild = client.GetGuild(guildId);
            var user = guild.GetUser(userId);
            var role = guild.GetRole(roleId);
            await user?.RemoveRoleAsync(role);
        }
        catch (Exception e)
        {
            Console.WriteLine($"[General/Warning] {DateTime.UtcNow:HH:mm:ss} RemoveRoleJob {e}");
        }
    }

    public static async Task Queue(string guildId, string userId, string roleId, DateTime schedule)
    {
        try
        {
            var removeRoleJob = JobBuilder.Create<RemoveRoleJob>()
                .WithIdentity($"removeRoleJob-{Guid.NewGuid()}", guildId)
                .UsingJobData("guildId", guildId)
                .UsingJobData("userId", userId)
                .UsingJobData("roleId", roleId)
                .Build();
            var removeRoleTrigger = TriggerBuilder.Create()
                .WithIdentity($"removeRoleTrigger-{Guid.NewGuid()}", guildId)
                .StartAt(schedule)
                .Build();
            if (schedule > DateTime.UtcNow.AddMinutes(10))
                await Quartz.PersistentSchedulerInstance.ScheduleJob(removeRoleJob, removeRoleTrigger);
            else
                await Quartz.MemorySchedulerInstance.ScheduleJob(removeRoleJob, removeRoleTrigger);
        }
        catch (Exception e)
        {
            Console.WriteLine($"[General/Warning] {DateTime.UtcNow:HH:mm:ss} RemoveRoleQueue {e}");
        }
    }
}