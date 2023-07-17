using System.Reflection;
using Discord;
using Discord.Interactions;
using Discord.Webhook;
using Discord.WebSocket;

namespace DougBot.SlashCommands;

public class MoveCmd : InteractionModuleBase
{
    [SlashCommand("move", "Move the message to a new channel")]
    [EnabledInDm(false)]
    [RequireUserPermission(GuildPermission.ModerateMembers)]
    public async Task Move(
        [Summary(description: "The ID of the message")]
        string message,
        [Summary(description: "The channel to move it to")]
        IChannel channelInput
    )
    {
        await RespondAsync("Moving the message", ephemeral: true);
        //Identify if it is a thread
        SocketThreadChannel? threadChannel = channelInput as SocketThreadChannel;
        var channel = threadChannel != null
            ? threadChannel.ParentChannel as ITextChannel
            : channelInput as ITextChannel;
        ulong? threadId = threadChannel != null ? threadChannel.Id : null;
        //Grab the message using a reply
        var messageToMove = await Context.Channel.GetMessageAsync(Convert.ToUInt64(message));
        //Check the message to move is not null
        if (messageToMove is null)
        {
            await ModifyOriginalResponseAsync(x => x.Content = "Message not found");
            return;
        }

        //Set the authors name as either the server nickname if there is one or the username
        var authorObj = messageToMove.Author as IGuildUser;
        var authorName = authorObj.Nickname ?? authorObj.Username;
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
        if (messageToMove.Attachments.Count > 0)
        {
            //Download message attachments from url via httpclient
            var attachments = new List<FileAttachment>();
            var attachmentPaths = new List<string>();
            using var httpClient = new HttpClient();
            //get root path
            var rootPath = Path.GetDirectoryName(Assembly.GetExecutingAssembly().Location);
            foreach (var attachment in messageToMove.Attachments)
            {
                var attachmentBytes = await httpClient.GetByteArrayAsync(attachment.Url);
                var path = Path.Combine(rootPath, attachment.Filename);
                await File.WriteAllBytesAsync(path, attachmentBytes);
                attachments.Add(new FileAttachment(path, attachment.Filename));
                attachmentPaths.Add(path);
            }

            //Send the message with attachments
            await webhook.SendFilesAsync(attachments, messageToMove.Content, embeds: embedList,
                username: authorName, avatarUrl: messageToMove.Author.GetAvatarUrl(),
                allowedMentions: AllowedMentions.None, threadId: threadId);
            //Delete the attachments
            foreach (var attachment in attachmentPaths) File.Delete(attachment);
        }
        else
        {
            await webhook.SendMessageAsync(messageToMove.Content, embeds: embedList,
                username: authorName, avatarUrl: messageToMove.Author.GetAvatarUrl(),
                allowedMentions: AllowedMentions.None, threadId: threadId);
        }

        await messageToMove.DeleteAsync();
        await ModifyOriginalResponseAsync(x => x.Content = "Message moved");
        string destination = threadChannel != null ? threadChannel.Mention : channel.Mention;
        await ReplyAsync($"{messageToMove.Author.Mention} your message has been moved to {destination}");
    }
}