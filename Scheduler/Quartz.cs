using System.Collections.Specialized;
using Microsoft.Azure.Cosmos;
using Quartz;
using Quartz.Impl;
using Quartz.Impl.Matchers;
using Quartz.Logging;
using Quartz.Spi.CosmosDbJobStore;

namespace DougBot.Scheduler;

public static class Quartz
{
    public static IScheduler SchedulerInstance { get; private set; }
    public static List<string> _FailedJobNames { get; set; } = new();
    public static async Task Initialize()
    {
        var properties = new NameValueCollection
        {
            [StdSchedulerFactory.PropertySchedulerInstanceName] = "QuartzScheduler",
            [StdSchedulerFactory.PropertySchedulerInstanceId] = $"DougBot",
            [StdSchedulerFactory.PropertyJobStoreType] = typeof(CosmosDbJobStore).AssemblyQualifiedName,
            [$"{StdSchedulerFactory.PropertyObjectSerializer}.type"] = "json",
            [$"{StdSchedulerFactory.PropertyJobStorePrefix}.Endpoint"] = Environment.GetEnvironmentVariable("ACCOUNT_ENDPOINT"),
            [$"{StdSchedulerFactory.PropertyJobStorePrefix}.Key"] = Environment.GetEnvironmentVariable("ACCOUNT_KEY"),
            [$"{StdSchedulerFactory.PropertyJobStorePrefix}.DatabaseId"] = Environment.GetEnvironmentVariable("DATABASE_NAME"),
            [$"{StdSchedulerFactory.PropertyJobStorePrefix}.CollectionId"] = "Quartz",
            [$"{StdSchedulerFactory.PropertyJobStorePrefix}.Clustered"] = "true",
            [$"{StdSchedulerFactory.PropertyJobStorePrefix}.ConnectionMode"] = ((int)ConnectionMode.Gateway).ToString()
        };
        //JobFactory
        LogProvider.SetCurrentLogProvider(new ConsoleLogProvider());
        var scheduler = await SchedulerBuilder.Create(properties)
            .UseDefaultThreadPool(tp => tp.MaxConcurrency = 20)
            .BuildScheduler();
        //Singleton
        SchedulerInstance = scheduler;
        //Start
        await scheduler.Start();
    }
    
    public static async Task CoreJobs()
    {
        //Wait for Quartz to start
        while (SchedulerInstance == null || SchedulerInstance.IsStarted == false)
        {
            await Task.Delay(1000);
        }
        //Clean jobs without a trigger
        var jobKeys = await SchedulerInstance.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
        foreach (var jobKey in jobKeys)
        {
            var triggers = await SchedulerInstance.GetTriggersOfJob(jobKey);
            if (triggers.Count == 0)
            {
                await SchedulerInstance.DeleteJob(jobKey);
            }
        }
        //Clean Forums
        var job = JobBuilder.Create<CleanForumsJob>()
            .WithIdentity("CleanForumsJob", "System")
            .Build();
        var trigger = TriggerBuilder.Create()
            .WithIdentity("CleanForumsTrigger", "System")
            .StartNow()
            .WithSimpleSchedule(x => x.WithIntervalInHours(1).RepeatForever())
            .Build();
        await SchedulerInstance.ScheduleJob(job, trigger);
        //Youtube
        job = JobBuilder.Create<CheckYoutubeJob>()
            .WithIdentity("YoutubeJob", "System")
            .Build();
        trigger = TriggerBuilder.Create()
            .WithIdentity("YoutubeTrigger", "System")
            .StartNow()
            .WithSimpleSchedule(x => x.WithIntervalInMinutes(10).RepeatForever())
            .Build();
        await SchedulerInstance.ScheduleJob(job, trigger);
    }
}

internal class ConsoleLogProvider : ILogProvider
{
    public Logger GetLogger(string name)
    {
        return (level, func, exception, parameters) =>
        {
            if (level >= LogLevel.Info && func != null )
            {
                Console.WriteLine("[" + DateTime.Now.ToLongTimeString() + "] [" + level + "] " + func(), parameters);
            }
            if (exception != null)
            {
                Console.WriteLine("[" + DateTime.Now.ToLongTimeString() + "] [" + level + "] " + exception);
                Quartz._FailedJobNames.Add(func().Split(' ')[1].Split(".")[1]);
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
 