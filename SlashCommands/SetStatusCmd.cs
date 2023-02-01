using System.Text.Json;
using Discord;
using Discord.Interactions;
using DougBot.Models;

namespace DougBot.SlashCommands;

public class SetStatusCmd : InteractionModuleBase
{
    [SlashCommand("setstatus", "Set the status of the bot")]
    [EnabledInDm(false)]
    [DefaultMemberPermissions(GuildPermission.ModerateMembers)]
    public async Task SetStatus([Summary(description: "What should the status be")] string status)
    {
        //Set the bots status
        var dict = new Dictionary<string, string>
        {
            { "status", status },
        };
        var json = JsonSerializer.Serialize(dict);
        var queue = new Queue()
        {
            Id = Guid.NewGuid().ToString(),
            Type = "SetStatus",
            Keys = json
        };
        await using var db = new Database.DougBotContext();
        await db.Queues.AddAsync(queue);
        await db.SaveChangesAsync();
        await RespondAsync($"Status set to `{status}` and will update shortly");
    }
}