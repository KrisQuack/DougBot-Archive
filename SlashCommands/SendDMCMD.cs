using System.Text.Json;
using Discord;
using Discord.Interactions;
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
        var json = JsonSerializer.Serialize(dict);
        var queue = new Queue()
        {
            Id = Guid.NewGuid().ToString(),
            Type = "SendDM",
            Keys = json
        };
        await using var db = new Database.DougBotContext();
        var dbGuild = await db.Guilds.FindAsync(Context.Guild.Id.ToString());
        await db.Queues.AddAsync(queue);
        await db.SaveChangesAsync();
        await RespondAsync($"DM queued, check <#{dbGuild.DmReceiptChannel}>", ephemeral: true);
    }
}