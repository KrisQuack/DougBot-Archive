using System.Diagnostics;
using Discord;
using Discord.Interactions;
using Quartz;
using Quartz.Impl.Matchers;

namespace DougBot.SlashCommands;

public class BotStatusCmd : InteractionModuleBase
{
    [SlashCommand("botstatus", "Displays the current status of the bot")]
    [EnabledInDm(false)]
    [DefaultMemberPermissions(GuildPermission.ModerateMembers)]
    public async Task BotStatus()
    {
        if (Context.Guild != null)
        {
            await RespondAsync("Processing", ephemeral: true);
            var process = Process.GetCurrentProcess();
            //Get uptime
            var uptime = DateTime.UtcNow - process.StartTime.ToUniversalTime();
            //Get memory usage; 
            var usedMemory = process.PrivateMemorySize64;
            var usedMemoryInMb = usedMemory / (1024 * 1024);
            //Get threads
            var currentAppThreadsCount = process.Threads.Count;
            var threadList = process.Threads.Cast<ProcessThread>().ToList();
            var youngThreads = threadList.Count(t => t.TotalProcessorTime.TotalSeconds < 10);
            // Get job keys
            var jobKeys =
                await Scheduler.Quartz.PersistentSchedulerInstance.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
            // Calculate job counts using LINQ
            var totalJobs = jobKeys.Count;
            //Create embed
            var embed = new EmbedBuilder()
                .WithTitle("Bot Status")
                .AddField("Uptime",
                    $"{uptime.Days}d {uptime.Hours}:{uptime.Minutes}:{uptime.Seconds}", true)
                .AddField("Memory Usage", $"{usedMemoryInMb} MB", true)
                .AddField("Threads", $"{currentAppThreadsCount}", true)
                .AddField("Young Threads (<10s)", $"{youngThreads}", true)
                .AddField("Quartz Job Data", "*These stats are since the bots last reboot*")
                .AddField("Total Jobs", totalJobs, true)
                .Build();
            await ModifyOriginalResponseAsync(m => m.Embeds = new[] { embed });
            await ModifyOriginalResponseAsync(m => m.Content = " ");
        }
    }
}