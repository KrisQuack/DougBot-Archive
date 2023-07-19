using Discord;
using Discord.Interactions;
using DougBot.Models;

namespace DougBot.SlashCommands;

public class SettingsCmd : InteractionModuleBase
{
    [SlashCommand("settings", "Change Settings")]
    [EnabledInDm(false)]
    [RequireUserPermission(GuildPermission.Administrator)]
    public async Task Settings(
        [Choice("Emotes", "emotes")] string setting,
        [Summary(description: "Leave empty to view current setting")]
        string? value = null
    )
    {
        await RespondAsync("Processing...", ephemeral: true);
        //Get guild
        var dbGuild = await Guild.GetGuild(Context.Guild.Id.ToString());
        //Get setting
        if (value == null)
            switch (setting)
            {
                case "emotes":
                    await ModifyOriginalResponseAsync(x =>
                        x.Content = $"{string.Join(",", dbGuild.ReactionFilterEmotes)}");
                    break;
            }
        //Set setting
        else
            switch (setting)
            {
                case "emotes":
                    var emotes = value.Split(",");
                    dbGuild.ReactionFilterEmotes = emotes;
                    await dbGuild.Update();
                    await ModifyOriginalResponseAsync(x =>
                        x.Content = $"Emotes set to: {string.Join(",", dbGuild.ReactionFilterEmotes)}");
                    break;
            }
    }
}