using System.Text;
using System.Text.RegularExpressions;
using Discord;
using Discord.Interactions;
using DougBot.Models;
using DougBot.Scheduler;

namespace DougBot.SlashCommands;

[Group("ai", "AI based commands")]
[EnabledInDm(false)]
[RequireUserPermission(GuildPermission.ModerateMembers)]
public class AiChatCmd : InteractionModuleBase
{
    [SlashCommand("analyse", "Analyses the current chat")]
    public async Task Analyze([Summary("read", "How many messages to read (50)")] [MaxValue(200)] int read = 50)
    {
        //Initial response
        var embed = new EmbedBuilder()
            .WithColor(Color.Blue)
            .WithTitle("Chat Analysis")
            .WithDescription("Processing, This may take a minute");
        await RespondAsync(embeds: new[] { embed.Build() }, ephemeral: true);
        //Get values
        var channel = Context.Channel as ITextChannel;
        var messages = await channel.GetMessagesAsync(200).FlattenAsync();
        //Filter messages to ignore, select number to read, and order by date
        var messageString = messages.Where(m =>
                !string.IsNullOrWhiteSpace(m.Content) &&
                !m.Author.IsBot
            ).Take(read).OrderBy(m => m.CreatedAt)
            .Aggregate("", (current, message) => current + $"\n{message.Author.Username}: {message.Content}");
        //Send to API
        try
        {
            var chatMessage =
                $@"You are an assistant who's job is to analyze a given conversation and provide a summary of what it is about and if any rules have been broken.
Consider the following rules for the conversation:
0) Follow Discord's Terms of Service and Community Guidelines.
1) Obey Moderation and Common Sense; disputes should be made in a ticket.
2) No Offensive Speech or harassment.
3) Be Kind; discourteous messages will be removed.
4) No Spam or Shitposting; follow channel rules.
5) English-Only conversations.
6) No Alt Accounts or Impersonation.
7) No Political Discussion; keep it light-hearted.
8) No Sexual Topics; occasional mature jokes allowed.
9) No Extremely Distressing topics; seek professional help for mental health issues.
Conversation:{messageString}".Trim();
            var response = EdgeGpt.Run(chatMessage, "precise").text;
            embed.WithDescription(response.Contains("<Disengaged>") ? "Bing refused to respond" : response);
            await ModifyOriginalResponseAsync(m => m.Embeds = new[] { embed.Build() });
        }
        catch (Exception e)
        {
            var response = "Failed to analyse chat, Please try again: " + e.Message;
            embed.WithDescription(response);
            await ModifyOriginalResponseAsync(m => m.Embeds = new[] { embed.Build() });
        }
    }

    [SlashCommand("ask", "Ask Bing a question")]
    public async Task Ask(string question)
    {
        //Initial response
        var embed = new EmbedBuilder()
            .WithColor(Color.Blue)
            .WithTitle("Ask Bing")
            .WithDescription("Processing, This may take a minute");
        await RespondAsync(embeds: new[] { embed.Build() }, ephemeral: true);
        //Send to API
        try
        {
            var response = EdgeGpt.Run(question, "precise").adaptive_text;
            embed.WithDescription(response.Contains("<Disengaged>") ? "Bing refused to respond" : response);
            await ModifyOriginalResponseAsync(m => m.Embeds = new[] { embed.Build() });
        }
        catch (Exception e)
        {
            var response = "Failed to chat, Please try again: " + e.Message;
            embed.WithDescription(response);
            await ModifyOriginalResponseAsync(m => m.Embeds = new[] { embed.Build() });
        }
    }

    [SlashCommand("chat", "Respond to messages in chat")]
    [RequireOwnerOrUserPermission(GuildPermission.Administrator)]
    public async Task Chat([Summary("read", "How many messages to read (50)")] [MaxValue(200)] int read = 50)
    {
        //Initial response
        var embed = new EmbedBuilder()
            .WithColor(Color.Blue)
            .WithTitle("AI Chatting")
            .WithDescription("Processing, This may take a minute");
        await RespondAsync(embeds: new[] { embed.Build() }, ephemeral: true);
        //Get values
        var channel = Context.Channel as ITextChannel;
        var messages = await channel.GetMessagesAsync(200).FlattenAsync();
        //Filter messages to ignore, select number to read, and order by date
        messages = messages.Where(m => !string.IsNullOrWhiteSpace(m.Content))
            .Take(read)
            .OrderBy(m => m.CreatedAt);
        var messageString = new StringBuilder();
        foreach (var message in messages)
        {
            messageString.Append($"{message.Author.Username}: ");
            if (message.Reference != null)
            {
                var referencedMessage = await channel.GetMessageAsync((ulong)message.Reference.MessageId);
                messageString.Append($"{referencedMessage.Author.Username}, ");
            }

            messageString.AppendLine(message.CleanContent.Replace("@", ""));
        }

        //Send to API
        try
        {
            var chatMessage =
                $"Reply to this conversation with one sentence as a user named WAHAHA.You may use search.\n{messageString}"
                    .Trim();
            var response = EdgeGpt.Run(chatMessage, "creative").text;
            //Clean up response
            response = response.Replace("WAHAHA:", "");
            response = response.Replace("Generating answers for you...", "");
            //Remove lines that contain "]: http", "Searching the web for:", or are empty
            response = Regex.Replace(response, @"(\[.*\]: http.*)|(Searching the web for:.*)|(\s*\n)", "",
                RegexOptions.Multiline);
            //Remove [^1^], [^2^], etc
            response = Regex.Replace(response, @"\[\^\d+\^\]", "");
            response = response.Trim();
            //Send Response
            embed.WithDescription(response);
            if (!response.Contains("<Disengaged>"))
                await SendMessageJob.Queue(Context.Guild.Id.ToString(), Context.Channel.Id.ToString(),
                    new List<EmbedBuilder>(), DateTime.UtcNow, response);
            else
                await ModifyOriginalResponseAsync(m => m.Embeds = new[] { embed.Build() });
        }
        catch (Exception e)
        {
            var response = "Failed to chat, Please try again: " + e.Message;
            embed.WithDescription(response);
            await ModifyOriginalResponseAsync(m => m.Embeds = new[] { embed.Build() });
        }
    }
}