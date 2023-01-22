using Discord.Interactions;
using Discord.WebSocket;
using DougBot.Models;

namespace DougBot.Modals;

public class AiChatInteraction : InteractionModuleBase
{
    [ComponentInteraction("aiChatApprove")]
    public async Task ComponentResponse()
    {
        var interaction = Context.Interaction as SocketMessageComponent;
        var message = interaction.Message.Embeds.First().Description.Split("\n")[3].Replace("**Response:** ", "");
        await ReplyAsync(message);
        await RespondAsync("Override sent!", ephemeral: true);
    }
}