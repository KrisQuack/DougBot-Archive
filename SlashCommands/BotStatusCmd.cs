using System.Diagnostics;
using Discord;
using Discord.Interactions;
using DougBot.Models;

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
            var process = Process.GetCurrentProcess();
            //Get uptime
            var uptime = DateTime.UtcNow - process.StartTime.ToUniversalTime();
            //Get memory usage; 
            var usedMemory = process.PrivateMemorySize64;
            var usedMemoryInMB = usedMemory / (1024 * 1024);
            //Get threads
            var threadsCount = Process.GetProcesses().Sum(p => p.Threads.Count);
            var currentAppThreadsCount = process.Threads.Count;
            //Get queue
            var queue = await Queue.GetQueues();
            var queueCount = queue.Count;
            var dueCount = queue.Count(q => q.DueAt < DateTime.UtcNow);
            var embed = new EmbedBuilder()
                .WithTitle("Bot Status")
                .AddField("Uptime", $"{uptime.Days} days {uptime.Hours} hours {uptime.Minutes} minutes {uptime.Seconds} seconds", true)
                .AddField("Memory Usage", $"{usedMemoryInMB} MB", true)
                .AddField("Threads", $"{currentAppThreadsCount} / {threadsCount}", true)
                .AddField("Pending Jobs", queueCount, true)
                .AddField("Due Jobs", dueCount, true)
                .Build();
            await RespondAsync(embeds: new[] { embed }, ephemeral: true);
        }
    }
}