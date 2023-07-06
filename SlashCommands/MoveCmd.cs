using Discord;
using Discord.Interactions;
using Discord.Webhook;
using DougBot.Models;
using Microsoft.VisualBasic;

namespace DougBot.SlashCommands;

public class MoveCmd : InteractionModuleBase
{
    [SlashCommand("move", "Move the message to a new channel")]
    [EnabledInDm(false)]
    [RequireUserPermission(GuildPermission.ModerateMembers)]
    public async Task Move(
        [Summary(description: "The ID of the message")] string message,
        [Summary(description: "Leave empty to view current setting")] ITextChannel channel
    )
    {
        await RespondAsync("Moving the message", ephemeral: true);
        //Grab the message using a reply
        var messageToMove = await Context.Channel.GetMessageAsync(Convert.ToUInt64(message));
        //Check the message to move is not null
        if (messageToMove is null)
        {
            await ModifyOriginalResponseAsync(x => x.Content = "Message not found");
            return;
        }
        //Check if the channel has a webhook named Wah
        var webhooks = await channel.GetWebhooksAsync();
        //If the webhook is null, create a new one
        var wahWebhook = webhooks.FirstOrDefault(x => x.Name == "Wahaha") ?? await channel.CreateWebhookAsync("Wahaha");
        //Grab the webhook
        var webhook = new DiscordWebhookClient(wahWebhook.Id, wahWebhook.Token);
        //Get the embeds
        var repliedEmbeds = messageToMove.Embeds;
        var embeds = repliedEmbeds.Select(e => e as Embed);
        var embedList = embeds.ToList();
        //Send the message to the webhook
        await webhook.SendMessageAsync(messageToMove.Content, embeds: embedList, username: messageToMove.Author.Username, avatarUrl: messageToMove.Author.GetAvatarUrl(),allowedMentions: AllowedMentions.None);
        await messageToMove.DeleteAsync();
        await ModifyOriginalResponseAsync(x => x.Content = "Message moved");
        await ReplyAsync($"{messageToMove.Author.Mention} your message has been moved to {channel.Mention}");
    }
}