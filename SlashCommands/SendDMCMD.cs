using System.Text.Json;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DougBot.Models;

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
        var dict = new Dictionary<string, string>
        {
            { "guildId", Context.Guild.Id.ToString() },
            { "userId", user.Id.ToString() },
            { "embedBuilders", embedJson },
            { "SenderId", Context.User.Id.ToString() }
        };
        await new Queue("SendDM", 0, dict, null).Insert();
        await RespondAsync("DM Queued", ephemeral: true);
    }
    
    [ComponentInteraction("dmRecieved:*:*")]
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
        await interaction.Message.DeleteAsync();
        await RespondAsync($"Message Sent to {guildName}");
    }
}