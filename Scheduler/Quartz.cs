using System.Collections.Specialized;
using Quartz;
using Quartz.Impl;
using Quartz.Logging;

namespace DougBot.Scheduler;

public static class Quartz
{
    public static IScheduler PersistentSchedulerInstance { get; private set; }
    public static IScheduler MemorySchedulerInstance { get; private set; }
    public static List<string> FailedJobNames { get; set; } = new();

    public static async Task InitializePersistent()
    {
        try
        {
            //JobFactory
            LogProvider.SetCurrentLogProvider(new ConsoleLogProvider());
            var scheduler = await SchedulerBuilder.Create()
                .UsePersistentStore(store =>
                {
                    store.UseProperties = true;
                    store.RetryInterval = TimeSpan.FromSeconds(15);
                    store.UsePostgres(sql =>
                    {
                        sql.ConnectionString = Environment.GetEnvironmentVariable("QUARTZ_CONNECTION_STRING");
                    });
                    store.UseJsonSerializer();
                })
                .UseDefaultThreadPool(tp => tp.MaxConcurrency = 20)
                .BuildScheduler();
            //Singleton
            PersistentSchedulerInstance = scheduler;
            //Start
            await scheduler.Start();
        }
        catch (Exception e)
        {
            Console.WriteLine($"[General/Warning] {DateTime.UtcNow:HH:mm:ss} PersistentScheduler {e}");
        }
    }

    public static async Task InitializeMemory()
    {
        try
        {
            //JobFactory
            LogProvider.SetCurrentLogProvider(new ConsoleLogProvider());
            var scheduler = await SchedulerBuilder.Create()
                .WithName("QuartzMemory")
                .UseDefaultThreadPool(tp => tp.MaxConcurrency = 20)
                .BuildScheduler();
            //Singleton
            MemorySchedulerInstance = scheduler;
            //Start
            await scheduler.Start();
        }
        catch (Exception e)
        {
            Console.WriteLine($"[General/Warning] {DateTime.UtcNow:HH:mm:ss} MemoryScheduler {e}");
        }
    }

    public static async Task CoreJobs()
    {
        //Wait for Quartz to start
        while (MemorySchedulerInstance == null || MemorySchedulerInstance.IsStarted == false) await Task.Delay(1000);
        //Clean Forums
        var job = JobBuilder.Create<CleanForumsJob>()
            .WithIdentity("CleanForumsJob", "System")
            .Build();
        var trigger = TriggerBuilder.Create()
            .WithIdentity("CleanForumsTrigger", "System")
            .StartNow()
            .WithSimpleSchedule(x => x.WithIntervalInHours(1).RepeatForever())
            .Build();
        await MemorySchedulerInstance.ScheduleJob(job, trigger);
        //Youtube
        job = JobBuilder.Create<CheckYoutubeJob>()
            .WithIdentity("YoutubeJob", "System")
            .Build();
        trigger = TriggerBuilder.Create()
            .WithIdentity("YoutubeTrigger", "System")
            .StartNow()
            .WithSimpleSchedule(x => x.WithIntervalInMinutes(10).RepeatForever())
            .Build();
        await MemorySchedulerInstance.ScheduleJob(job, trigger);
    }
}

internal class ConsoleLogProvider : ILogProvider
{
    public Logger GetLogger(string name)
    {
        return (level, func, exception, parameters) =>
        {
            if (level >= LogLevel.Info && func != null)
                Console.WriteLine($"[General/Info] {DateTime.UtcNow:HH:mm:ss} {func()}", parameters);
            if (exception != null)
            {
                Console.WriteLine("[" + DateTime.UtcNow.ToLongTimeString() + "] [" + level + "] " + exception);
                Quartz.FailedJobNames.Add(func().Split(' ')[1].Split(".")[1]);
            }

            return true;
        };
    }

    public IDisposable OpenNestedContext(string message)
    {
        throw new NotImplementedException();
    }

    public IDisposable OpenMappedContext(string key, object value, bool destructure = false)
    {
        throw new NotImplementedException();
    }
}