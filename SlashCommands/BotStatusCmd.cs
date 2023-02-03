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
            var queue = await Queue.GetQueues();
            var QueueCount = queue.Count;
            var DueCount = queue.Count(q => q.DueAt < DateTime.UtcNow);
            var embed = new EmbedBuilder()
                .WithTitle("Bot Status")
                .AddField("Uptime", uptime.ToString("hh\\:mm\\:ss"))
                .AddField("Pending Jobs", QueueCount, true)
                .AddField("Due Jobs", DueCount, true)
                .Build();
            await RespondAsync(embeds: new[] { embed });
        }
    }
}