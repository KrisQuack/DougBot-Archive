using Discord.Interactions;
using Quartz;
using Quartz.Impl.Matchers;

namespace DougBot.SlashCommands;

[Group("owner", "Owner commands")]
[EnabledInDm(false)]
[RequireOwner]
public class OwnerCmd : InteractionModuleBase
{
    [SlashCommand("cleanqueue", "Clean the Quartz Queue")]
    public async Task CleanQueue()
    {
        await RespondAsync("Processing", ephemeral: true);
        var jobKeys = await Scheduler.Quartz.SchedulerInstance.GetJobKeys(GroupMatcher<JobKey>.AnyGroup());
        foreach (var jobKey in jobKeys)
        {
            var triggers = await Scheduler.Quartz.SchedulerInstance.GetTriggersOfJob(jobKey);
            if (triggers.Count == 0)
            {
                await Scheduler.Quartz.SchedulerInstance.DeleteJob(jobKey);
            }
        }
        await ModifyOriginalResponseAsync(m => m.Content = $"Removed {jobKeys.Count} jobs");
    }
}