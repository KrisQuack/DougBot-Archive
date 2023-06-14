using Quartz;

namespace DougBot.Scheduler;

public class AddRoleJob : IJob
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
            await user?.AddRoleAsync(role);
        }
        catch (Exception e)
        {
            Console.WriteLine($"[General/Warning] {DateTime.UtcNow:HH:mm:ss} AddRoleJob {e}");
        }
    }

    public static async Task Queue(string guildId, string userId, string roleId, DateTime schedule)
    {
        try
        {
            var addRoleJob = JobBuilder.Create<AddRoleJob>()
                .WithIdentity($"addRoleJob-{Guid.NewGuid()}", guildId)
                .UsingJobData("guildId", guildId)
                .UsingJobData("userId", userId)
                .UsingJobData("roleId", roleId)
                .Build();
            var addRoleTrigger = TriggerBuilder.Create()
                .WithIdentity($"addRoleTrigger-{Guid.NewGuid()}", guildId)
                .StartAt(schedule)
                .Build();
            if (schedule > DateTime.UtcNow.AddMinutes(10))
                await Quartz.PersistentSchedulerInstance.ScheduleJob(addRoleJob, addRoleTrigger);
            else
                await Quartz.MemorySchedulerInstance.ScheduleJob(addRoleJob, addRoleTrigger);
        }
        catch (Exception e)
        {
            Console.WriteLine($"[General/Warning] {DateTime.UtcNow:HH:mm:ss} AddRoleQueue {e}");
        }
    }
}