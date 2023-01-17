using Discord.Interactions;
using DougBot.Models;

namespace DougBot.Modals;

public class YoutubeInteraction : InteractionModuleBase
{
    [ModalInteraction("settingsYoutubeFeed")]
    public async Task ModalResponse(YoutubeModal modal)
    {
        Setting.UpdateYoutubeFeed(modal.YoutubePostChannel, modal.youtubeChannels);
        await RespondAsync("Settings Saved", ephemeral: true);
    }
}