using Discord.Interactions;
using DougBot.Models;

namespace DougBot.Modals;

public class ReactionFilterInteraction : InteractionModuleBase
{
    [ModalInteraction("settingsReactionFilter")]
    public async Task ModalResponse(ReactionFilterModal modal)
    {
        Setting.UpdateReactionFilter(modal.reactionEmotes, modal.reactionChannels, modal.reactionRole);
        await RespondAsync("Settings Saved", ephemeral: true);
    }
}