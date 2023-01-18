using Discord.Interactions;
using DougBot.Models;

namespace DougBot.Modals;

public class AdminInteraction : InteractionModuleBase
{
    [ModalInteraction("settingsAdmin")]
    public async Task ModalResponse(AdminModal modal)
    {
        Setting.UpdateAdmin(modal.OpenAiToken, modal.dmReceiptChannel, modal.OpenAiWordBlacklist);
        await RespondAsync("Settings Saved", ephemeral: true);
    }
}