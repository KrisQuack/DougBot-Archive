using Discord;
using Discord.Interactions;
using DougBot.Models;
using DougBot.Scheduler;

namespace DougBot.SlashCommands;

public class SendDmCmd : InteractionModuleBase
{
    [SlashCommand("senddm", "Send a DM to the specified user")]
    [EnabledInDm(false)]
    [RequireUserPermission(GuildPermission.ModerateMembers)]
    public async Task SendDm([Summary(description: "User to DM")] IGuildUser user,
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
        await SendDmJob.Queue(user.Id.ToString(), Context.User.Id.ToString(), Context.Guild.Id.ToString(),
            new List<EmbedBuilder> { embed }, DateTime.UtcNow);
        await RespondAsync("DM Queued", ephemeral: true);
    }

    [ComponentInteraction("dmRecieved:*:*", true)]
    public async Task DmProcess(string guildId, string guildName)
    {
        var interaction = Context.Interaction as IComponentInteraction;
        var dbGuild = await Guild.GetGuild(guildId);
        if (guildId == "cancel")
        {
            await interaction.Message.DeleteAsync();
            return;
        }

        var rawEmbeds = interaction.Message.Embeds;
        //Convert rawEmbeds to a list of EmbedBuilders
        var embedBuilders = rawEmbeds.Select(rawEmbed =>
            new EmbedBuilder()
                .WithAuthor(rawEmbed.Author.Value.Name, rawEmbed.Author.Value.IconUrl)
                .WithDescription(rawEmbed.Description)
                .WithColor(rawEmbed.Color.Value)
                .WithTimestamp(rawEmbed.Timestamp.Value)
                .WithImageUrl(rawEmbed.Image?.Url)
                .WithTitle(rawEmbed.Title)
        ).ToList();
        await SendMessageJob.Queue(dbGuild.Id, dbGuild.DmReceiptChannel, embedBuilders, DateTime.UtcNow);
        await interaction.Message.DeleteAsync();
        await RespondAsync($"Message Sent to {guildName}");
    }
}