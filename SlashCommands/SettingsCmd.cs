using System.Text.Json;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DougBot.Modals;
using DougBot.Models;

namespace DougBot.SlashCommands;

public class SettingsCmd : InteractionModuleBase
{
    [SlashCommand("settings", "Change bot settings")]
    [EnabledInDm(false)]
    [DefaultMemberPermissions(GuildPermission.ModerateMembers)]
    public async Task settings(
        [Choice("Reaction Filter", "reactionFilter")]
        [Choice("Youtube Feed", "youtubeFeed")]
        [Choice("Dump", "dump")]
        [Choice("Admin", "admin")]
        string choice)
    {
        var settings = Setting.GetSettings();
        switch (choice)
        {
            case "reactionFilter":
            {
                await Context.Interaction.RespondWithModalAsync<ReactionFilterModal>("settingsReactionFilter", null,
                        x => x
                            .UpdateTextInput("reactionEmotes", x => x.Value = settings.reactionFilterEmotes)
                            .UpdateTextInput("reactionChannels", x => x.Value = settings.reactionFilterChannels)
                            .UpdateTextInput("reactionRole", x => x.Value = settings.reactionFilterRole))
                    .ConfigureAwait(false);
                break;
            }
            case "youtubeFeed":
            {
                await Context.Interaction.RespondWithModalAsync<YoutubeModal>("settingsYoutubeFeed", null, x => x
                        .UpdateTextInput("youtubeChannels", x => x.Value = settings.YoutubeChannels)
                        .UpdateTextInput("YoutubePostChannel", x => x.Value = settings.YoutubePostChannel))
                    .ConfigureAwait(false);
                break;
            }
            case "admin":
                var user = (SocketGuildUser)Context.User;
                if (user.GuildPermissions.Administrator)
                {
                    await Context.Interaction.RespondWithModalAsync<AdminModal>("settingsAdmin", null, x => x
                            .UpdateTextInput("dmReceiptChannel", x => x.Value = settings.dmReceiptChannel)
                            .UpdateTextInput("OpenAiToken", x => x.Value = settings.OpenAiToken))
                        .ConfigureAwait(false);
                    break;
                }

                RespondAsync("Access denied", ephemeral: true);
                break;
            case "dump":
            {
                //Settings
                settings.Token = "REDACTED";
                var emotes = settings.reactionFilterEmotes;
                settings.reactionFilterEmotes = "!emote!";
                var jsonString = JsonSerializer.Serialize(settings, new JsonSerializerOptions { WriteIndented = true });
                jsonString = jsonString.Replace("!emote!", emotes);
                await Context.Channel.SendMessageAsync($"```json\n{jsonString}```");
                //Queue

                break;
            }
        }
    }
}