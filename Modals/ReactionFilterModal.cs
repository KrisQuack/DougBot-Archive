using Discord;
using Discord.Interactions;

namespace DougBot.Modals;

public class ReactionFilterModal : IModal
{
    [InputLabel("Allowed Emotes")]
    [ModalTextInput("reactionEmotes", TextInputStyle.Paragraph)]
    public string reactionEmotes { get; set; }

    [InputLabel("Filtered Channels")]
    [ModalTextInput("reactionChannels", TextInputStyle.Paragraph)]
    public string reactionChannels { get; set; }

    [InputLabel("Filter Role")]
    [ModalTextInput("reactionRole")]
    public string reactionRole { get; set; }

    public string Title => "Settings: Reaction Filter";
}