using System.Text.Json;
using Discord;
using Discord.Interactions;
using DougBot.Models;
using DougBot.Scheduler;
using Quartz;
using JsonSerializerOptions = System.Text.Json.JsonSerializerOptions;

namespace DougBot.SlashCommands;

public class SendDMCMD : InteractionModuleBase
{
    [SlashCommand("senddm", "Send a DM to the specified user")]
    [EnabledInDm(false)]
    [DefaultMemberPermissions(GuildPermission.ModerateMembers)]
    public async Task SendDM([Summary(description: "User to DM")] IGuildUser user,
        [Summary(description: "Message to send")]
        string message)
    {
        var embed = new EmbedBuilder()
            .WithDescription(message)
            .WithColor(Color.Orange)
            .WithAuthor(new EmbedAuthorBuilder()
                .WithName(Context.Guild.Name + " Mods")
                .WithIconUrl(Context.Guild.IconUrl))
            .WithCurrentTimestamp()
            .WithFooter(new EmbedFooterBuilder()
                .WithText("Any replies to this DM will be sent to the mod team"));
        var embedJson = JsonSerializer.Serialize(new List<EmbedBuilder> { embed },
            new JsonSerializerOptions { Converters = { new ColorJsonConverter() } });
        var sendDmJob = JobBuilder.Create<SendDmJob>()
            .WithIdentity($"sendDMJob-{Guid.NewGuid()}", Context.Guild.Id.ToString())
            .UsingJobData("guildId", Context.Guild.Id.ToString())
            .UsingJobData("userId", user.Id.ToString())
            .UsingJobData("embedBuilders", embedJson)
            .UsingJobData("senderId", Context.User.Id.ToString())
            .Build();
        var sendDmTrigger = TriggerBuilder.Create()
            .WithIdentity($"sendDMTrigger-{Guid.NewGuid()}", Context.Guild.Id.ToString())
            .StartNow()
            .Build();
        await Scheduler.Quartz.MemorySchedulerInstance.ScheduleJob(sendDmJob, sendDmTrigger);
        await RespondAsync("DM Queued", ephemeral: true);
    }

    [ComponentInteraction("dmRecieved:*:*",true)]
    public async Task DMProcess(string guildId, string guildName)
    {
        var interaction = Context.Interaction as IComponentInteraction;
        var dbGuild = await Guild.GetGuild(guildId);
        if (guildId == "cancel")
        {
            await interaction.Message.DeleteAsync();
            return;
        }

        var embedJson = JsonSerializer.Serialize(interaction.Message.Embeds,
            new JsonSerializerOptions { Converters = { new ColorJsonConverter() } });
        var sendMessageJob = JobBuilder.Create<SendMessageJob>()
            .WithIdentity($"sendMessageJob-{Guid.NewGuid()}", dbGuild.Id)
            .UsingJobData("guildId", dbGuild.Id)
            .UsingJobData("channelId", dbGuild.DmReceiptChannel)
            .UsingJobData("message", "")
            .UsingJobData("embedBuilders", embedJson)
            .UsingJobData("ping", "false")
            .UsingJobData("attachments", null)
            .Build();
        var sendMessageTrigger = TriggerBuilder.Create()
            .WithIdentity($"sendMessageTrigger-{Guid.NewGuid()}", dbGuild.Id)
            .StartNow()
            .Build();
        await Scheduler.Quartz.MemorySchedulerInstance.ScheduleJob(sendMessageJob, sendMessageTrigger);
        await interaction.Message.DeleteAsync();
        await RespondAsync($"Message Sent to {guildName}");
    }
}