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
    [RequireUserPermission(GuildPermission.ModerateMembers)]
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
            var persistentKeys = await Scheduler.Quartz.PersistentSchedulerInstance.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
            var memoryKeys = await Scheduler.Quartz.MemorySchedulerInstance.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
            //Create embed
            var embed = new EmbedBuilder()
                .WithTitle("Bot Status")
                .AddField("Uptime",
                    $"{uptime.Days}d {uptime.Hours}:{uptime.Minutes}:{uptime.Seconds}", true)
                .AddField("Memory Usage", $"{usedMemoryInMb} MB", true)
                .AddField("Threads", $"{currentAppThreadsCount}", true)
                .AddField("Young Threads (<10s)", $"{youngThreads}", true)
                .AddField("Quartz Job Data", "*These stats are since the bots last reboot*")
                .AddField("Total Persistent Jobs", persistentKeys.Count, true)
                .AddField("Total Memory Jobs", memoryKeys.Count, true)
                .AddField("Twitch Bot", "*Details about the twitch connection*")
                .AddField("IRC Initialized", Twitch.Twitch.IRC.IsInitialized ? "Yes" : "No", true)
                .AddField("IRC Connected", Twitch.Twitch.IRC.IsConnected ? "Yes" : "No", true)
                .AddField("IRC Channels", Twitch.Twitch.IRC.JoinedChannels.Count, true)
                .Build();
            await ModifyOriginalResponseAsync(m => m.Embeds = new[] { embed });
            await ModifyOriginalResponseAsync(m => m.Content = " ");
        }
    }
}