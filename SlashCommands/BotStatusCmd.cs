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
            var uptime = DateTime.UtcNow - Process.GetCurrentProcess().StartTime.ToUniversalTime();
            var QueueCount = Queue.GetAll().Count;
            var DueCount = Queue.GetDue().Count;
            var embed = new EmbedBuilder()
                .WithTitle("Bot Status")
                .AddField("Uptime", uptime.ToString("hh\\:mm\\:ss"))
                .AddField("Threads", Process.GetCurrentProcess().Threads.Count)
                .AddField("Pending Jobs", QueueCount)
                .AddField("Due Jobs", DueCount)
                .Build();
            await RespondAsync(embeds: new[] { embed });
        }
    }
}