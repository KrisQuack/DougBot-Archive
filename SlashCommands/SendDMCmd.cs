using Discord;
using Discord.Interactions;
using DougBot.Models;
namespace DougBot.SlashCommands;

public class SendDMCmd : InteractionModuleBase
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
        user.SendMessageAsync(embed: embed.Build());
        embed.Author.Name = $"DM to {user.Username} ({user.Id}) from {Context.User.GlobalName}";
        embed.Author.IconUrl = Context.User.GetAvatarUrl();
        await ConfigurationService.Instance.DmReceiptChannel.SendMessageAsync(embed: embed.Build());
        await RespondAsync("DM Sent", ephemeral: true);
    }
}