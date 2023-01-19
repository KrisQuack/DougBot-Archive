using Discord.Interactions;

namespace DougBot.Modals;

public class AdminModal : IModal
{
    [InputLabel("DM Channel")]
    [ModalTextInput("dmReceiptChannel")]
    public string dmReceiptChannel { get; set; }

    [InputLabel("OpenAI Token")]
    [ModalTextInput("OpenAiToken")]
    public string OpenAiToken { get; set; }
    
    [InputLabel("OpenAI Word Blacklist")]
    [ModalTextInput("OpenAiWordBlacklist")]
    public string OpenAiWordBlacklist { get; set; }

    public string Title => "Settings: Admin Settings";
}