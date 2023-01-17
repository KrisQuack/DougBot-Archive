using Discord;
using Discord.Interactions;

namespace DougBot.Modals;

public class YoutubeModal : IModal
{
    [InputLabel("Ping Channel")]
    [ModalTextInput("YoutubePostChannel")]
    public string YoutubePostChannel { get; set; }

    [InputLabel("YoutubeChannels (Channel;PingRole)")]
    [ModalTextInput("youtubeChannels", TextInputStyle.Paragraph)]
    public string youtubeChannels { get; set; }

    public string Title => "Settings: Youtube Feed";
}