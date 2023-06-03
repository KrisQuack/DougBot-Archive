using System.Text;
using BingChat;
using Discord;
using Discord.Interactions;
using DougBot.Scheduler;

namespace DougBot.SlashCommands;

[Group("ai", "AI based commands")]
[EnabledInDm(false)]
[RequireUserPermission(GuildPermission.ModerateMembers)]
public class AIChatCmd : InteractionModuleBase
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
            //get random number as string
            var randomString = Program.Random.Next(100000000, 999999999).ToString();
            var client = new BingChatClient(new BingChatClientOptions
            {
                CookieU = randomString,
                Tone = BingChatTone.Precise
            });
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
            var response = await client.AskAsync(chatMessage);
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
            //get random number as string
            var randomString = Program.Random.Next(100000000, 999999999).ToString();
            var client = new BingChatClient(new BingChatClientOptions
            {
                CookieU = randomString,
                Tone = BingChatTone.Precise
            });
            var response = await client.AskAsync(question);
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
    [RequireUserPermission(GuildPermission.Administrator)]
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
            messageString.Append($"{message.Author.Mention}: ");
            if (message.Reference != null)
            {
                var referencedMessage = await channel.GetMessageAsync((ulong)message.Reference.MessageId);
                messageString.Append($"{referencedMessage.Author.Mention}, ");
            }
            messageString.AppendLine(message.Content);
        }
        //Send to API
        try
        {
            var client = new BingChatClient(new BingChatClientOptions
            {
                Tone = BingChatTone.Creative
            });
            var chatMessage =$"Act as a discord user named <@1037302561058848799> nicknamed Wah and reply to this conversation with one sentence.\n{messageString}".Trim();
            var response = await client.AskAsync(chatMessage);
            response = response.Replace("<@1037302561058848799>:", "").Replace("<@!1037302561058848799>:", "");
            embed.WithDescription(response);
            if(!response.Contains("<Disengaged>"))
                await SendMessageJob.Queue(Context.Guild.Id.ToString(), Context.Channel.Id.ToString(), new List<EmbedBuilder>(), DateTime.UtcNow, response);
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