using DougBot.Systems.TimeBased;
using Quartz;
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