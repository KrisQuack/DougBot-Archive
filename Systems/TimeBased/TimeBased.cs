using Quartz;

namespace DougBot.Systems.TimeBased;

public static class TimeBased
{

    public static async Task Schedule()
    {
        //Wait for Quartz to start
        while (Scheduler.Quartz.MemorySchedulerInstance == null || Scheduler.Quartz.MemorySchedulerInstance.IsStarted == false) await Task.Delay(1000);
        //Clean Forums
        var job = JobBuilder.Create<CleanForumsJob>()
            .WithIdentity("CleanForumsJob", "System")
            .Build();
        var trigger = TriggerBuilder.Create()
            .WithIdentity("CleanForumsTrigger", "System")
            .StartNow()
            .WithSimpleSchedule(x => x.WithIntervalInHours(1).RepeatForever())
            .Build();
        await Scheduler.Quartz.MemorySchedulerInstance.ScheduleJob(job, trigger);
        //Youtube
        job = JobBuilder.Create<CheckYoutubeJob>()
            .WithIdentity("YoutubeJob", "System")
            .Build();
        trigger = TriggerBuilder.Create()
            .WithIdentity("YoutubeTrigger", "System")
            .StartNow()
            .WithSimpleSchedule(x => x.WithIntervalInMinutes(10).RepeatForever())
            .Build();
        await Scheduler.Quartz.MemorySchedulerInstance.ScheduleJob(job, trigger);
        //Reaction Filter Frequent
        job = JobBuilder.Create<ReactionFilterJob>()
            .WithIdentity("ReactionFilterJob", "System")
            .UsingJobData("messageCount", 10)
            .Build();
        trigger = TriggerBuilder.Create()
            .WithIdentity("ReactionFilterTrigger", "System")
            .StartNow()
            .WithSimpleSchedule(x => x.WithIntervalInMinutes(5).RepeatForever())
            .Build();
        await Scheduler.Quartz.MemorySchedulerInstance.ScheduleJob(job, trigger);
        //Reaction Filter Infrequent
        job = JobBuilder.Create<ReactionFilterJob>()
            .WithIdentity("ReactionFilterJob", "System")
            .UsingJobData("messageCount", 100)
            .Build();
        trigger = TriggerBuilder.Create()
            .WithIdentity("ReactionFilterTrigger", "System")
            .StartNow()
            .WithSimpleSchedule(x => x.WithIntervalInHours(6).RepeatForever())
            .Build();
        await Scheduler.Quartz.MemorySchedulerInstance.ScheduleJob(job, trigger);
    }
}