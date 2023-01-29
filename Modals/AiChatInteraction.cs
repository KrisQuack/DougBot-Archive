using Discord.Interactions;
using Discord.WebSocket;

namespace DougBot.Modals;

public class AiChatInteraction : InteractionModuleBase
{
    [ComponentInteraction("aiChatApprove")]
    public async Task ComponentResponse()
    {
        var interaction = Context.Interaction as SocketMessageComponent;
        var message = interaction.Message.Content.Split("\n")[1].Replace("Response: ", "");
        await ReplyAsync(message);
        await RespondAsync("Override sent!", ephemeral: true);
    }
}