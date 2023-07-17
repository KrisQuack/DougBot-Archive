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
        IChannel channel
    )
    {
        await RespondAsync("Moving the message", ephemeral: true);
        //Identify if it is a thread
        SocketThreadChannel? threadChannel = channel as SocketThreadChannel;
        ITextChannel? textChannel = null;
        IForumChannel? forumChannel = null;
        if (threadChannel != null)
        {
            textChannel = threadChannel.ParentChannel as ITextChannel;
            forumChannel = threadChannel.ParentChannel as IForumChannel;
        }
        else
        {
            textChannel = channel as ITextChannel;
            forumChannel = channel as IForumChannel;
        }
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
        //Get the webhook
        IWebhook wahWebhook;
        if (textChannel != null)
        {
            var webhooks = await textChannel.GetWebhooksAsync();
            wahWebhook = webhooks.FirstOrDefault(x => x.Name == "Wahaha") ?? await textChannel.CreateWebhookAsync("Wahaha");
        }
        else if (forumChannel != null)
        {
            var webhooks = await forumChannel.GetWebhooksAsync();
            wahWebhook = webhooks.FirstOrDefault(x => x.Name == "Wahaha") ?? await forumChannel.CreateWebhookAsync("Wahaha");
        }
        else
        {
            await ModifyOriginalResponseAsync(x => x.Content = "Invalid channel type. Only text channels, forum channels and threads are supported.");
            return;
        }
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
                allowedMentions: AllowedMentions.None, threadId: threadChannel.Id);
            //Delete the attachments
            foreach (var attachment in attachmentPaths) File.Delete(attachment);
        }
        else
        {
            await webhook.SendMessageAsync(messageToMove.Content, embeds: embedList,
                username: authorName, avatarUrl: messageToMove.Author.GetAvatarUrl(),
                allowedMentions: AllowedMentions.None, threadId: threadChannel.Id);
        }

        await messageToMove.DeleteAsync();
        await ModifyOriginalResponseAsync(x => x.Content = "Message moved");
        await ReplyAsync($"{messageToMove.Author.Mention} your message has been moved to <#{channel.Id}>");
    }
}