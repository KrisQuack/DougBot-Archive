using System.Text.Json;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DougBot.Models;
using DougBot.Scheduler;
using Fernandezja.ColorHashSharp;
using Quartz;
using JsonSerializerOptions = System.Text.Json.JsonSerializerOptions;

namespace DougBot.SlashCommands;
public class ReportCmd : InteractionModuleBase
{
    [MessageCommand("Report Message")]
    [EnabledInDm(false)]
    public async Task ReportMessage(IMessage message)
    {
        var channel = message.Channel as SocketGuildChannel;
        await RespondWithModalAsync<reportModal>($"report:{message.Id}:0");
    }

    [UserCommand("Report User")]
    [EnabledInDm(false)]
    public async Task ReportUser(IGuildUser user)
    {
        await RespondWithModalAsync<reportModal>($"report:0:{user.Id}");
    }

    [ModalInteraction("report:*:*",true)]
    public async Task ReportProcess(string messageID, string userID, reportModal modal)
    {
        await RespondAsync("Submitting...", ephemeral: true);
        try
        {
            var dbGuild = await Guild.GetGuild(Context.Guild.Id.ToString());
            //Get the report
            var colorHash = new ColorHash();
            var color = colorHash.BuildToColor(Context.User.Id.ToString());
            var embeds = new List<EmbedBuilder>();
            //If the message is a user report
            if (userID != "0")
            {
                var reportedUser = await Context.Guild.GetUserAsync(ulong.Parse(userID));
                embeds.Add(new EmbedBuilder()
                    .WithTitle("User Reported")
                    .WithFields(
                        new EmbedFieldBuilder()
                            .WithName("User Info")
                            .WithValue(
                                $"\nMention: {reportedUser.Mention}\nUsername: {reportedUser.Username}#{reportedUser.Discriminator}\nID: {reportedUser.Id}"),
                        new EmbedFieldBuilder()
                            .WithName("Reason")
                            .WithValue(modal.Reason)
                    )
                    .WithColor((Color)color)
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName($"{Context.User.Username}#{Context.User.Discriminator} ({Context.User.Id})")
                        .WithIconUrl(Context.User.GetAvatarUrl()))
                    .WithCurrentTimestamp());
            }
            //If the message is a message report
            else
            {
                var channel = Context.Channel as SocketTextChannel;
                var message = await channel.GetMessageAsync(ulong.Parse(messageID));
                embeds.Add(new EmbedBuilder()
                    .WithTitle("Message Reported")
                    .WithUrl(message.GetJumpUrl())
                    .WithFields(
                        new EmbedFieldBuilder()
                            .WithName("User Info")
                            .WithValue(
                                $"\nMention: {message.Author.Mention}\nUsername: {message.Author.Username}#{message.Author.Discriminator}\nID: {message.Author.Id}"),
                        new EmbedFieldBuilder()
                            .WithName("Message Info")
                            .WithValue($"\nChannel: {channel.Mention}\nMessage: {message.Content}"),
                        new EmbedFieldBuilder()
                            .WithName("Reason")
                            .WithValue(modal.Reason)
                    )
                    .WithColor((Color)color)
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName($"{Context.User.Username}#{Context.User.Discriminator} ({Context.User.Id})")
                        .WithIconUrl(Context.User.GetAvatarUrl()))
                    .WithCurrentTimestamp());
                //Attachment embeds
                embeds.AddRange(message.Attachments.Select(attachment =>
                    new EmbedBuilder().WithTitle(attachment.Filename).WithImageUrl(attachment.Url)
                        .WithUrl(attachment.Url)));
            }

            var embedJson = JsonSerializer.Serialize(embeds,
                new JsonSerializerOptions { Converters = { new ColorJsonConverter() } });
            var sendMessageJob = JobBuilder.Create<SendMessageJob>()
                .WithIdentity($"sendMessageJob-{Guid.NewGuid()}", dbGuild.Id)
                .UsingJobData("guildId", dbGuild.Id)
                .UsingJobData("channelId", dbGuild.ReportChannel)
                .UsingJobData("message", "")
                .UsingJobData("embedBuilders", embedJson)
                .UsingJobData("ping", "false")
                .UsingJobData("attachments", null)
                .Build();
            var sendMessageTrigger = TriggerBuilder.Create()
                .WithIdentity($"sendMessageTrigger-{Guid.NewGuid()}", dbGuild.Id)
                .StartNow()
                .Build();
            await Scheduler.Quartz.SchedulerInstance.ScheduleJob(sendMessageJob, sendMessageTrigger);
            await ModifyOriginalResponseAsync(m => m.Content = "Your report has been sent to the mods.");
        }
        catch (Exception e)
        {
            await ModifyOriginalResponseAsync(m =>
                m.Content = "An error occured while submitting this request, Please try again or open a ticket.");
            throw;
        }
    }
}

public class reportModal : IModal
{
    [ModalTextInput("reason", TextInputStyle.Paragraph,
        "Please explain what it is you are reporting and any details that may help us.")]
    public string Reason { get; set; }

    public string Title => "Generate Report";
}