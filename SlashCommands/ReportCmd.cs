using System.Text.Json;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DougBot.Models;
using Fernandezja.ColorHashSharp;

namespace DougBot.SlashCommands;

public class ReportCmd : InteractionModuleBase
{
    [MessageCommand("Report Message")]
    [EnabledInDm(false)]
    public async Task ReportMessage (IMessage message)
    {
        var channel = message.Channel as SocketGuildChannel;
        await RespondWithModalAsync<reportModal>($"report:{channel.Id}:{message.Id}:0");
    }
    
    [UserCommand("Report User")]
    [EnabledInDm(false)]
    public async Task ReportUser (IGuildUser user)
    {
        await RespondWithModalAsync<reportModal>($"report:0:0:{user.Id}");
    }
    
    [ModalInteraction("report:*:*:*")]
    public async Task ReportProcess(string channelID, string messageID, string userID, reportModal modal)
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
                    .WithValue($"\nMention: {reportedUser.Mention}\nUsername: {reportedUser.Username}#{reportedUser.Discriminator}\nID: {reportedUser.Id}"),
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
                .WithDescription($"Reason: {modal.Reason}")
                .WithUrl(message.GetJumpUrl())
                .WithFields(
                    new EmbedFieldBuilder()
                    .WithName("User Info")
                    .WithValue($"\nMention: {message.Author.Mention}\nUsername: {message.Author.Username}#{message.Author.Discriminator}\nID: {message.Author.Id}"),
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
                new EmbedBuilder().WithTitle(attachment.Filename).WithImageUrl(attachment.Url).WithUrl(attachment.Url)));
        }
        var embedJson = JsonSerializer.Serialize(embeds,
            new JsonSerializerOptions { Converters = { new ColorJsonConverter() } });
        var dict = new Dictionary<string, string>
        {
            { "guildId", dbGuild.Id },
            { "channelId", dbGuild.DmReceiptChannel },
            { "message", "" },
            { "embedBuilders", embedJson },
            { "ping", "false" },
            { "attachments", null }
        };
        await new Queue("SendMessage", null, dict, null).Insert();
    }
}
public class reportModal : IModal
{
    public string Title => "Generate Report";

    [ModalTextInput("reason", TextInputStyle.Paragraph, "Please explain what it is you are reporting an any details that may help us.")]
    public string Reason { get; set; }
}