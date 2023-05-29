using Discord;
using Discord.Interactions;
using BingChat;

namespace DougBot.SlashCommands;

[Group("ai", "AI based commands")]
[EnabledInDm(false)]
[DefaultMemberPermissions(GuildPermission.ModerateMembers)]
public class AIChatCmd : InteractionModuleBase
{
    [SlashCommand("analyse", "Analyses the current chat")]
    public async Task Analyze([Summary("read", "How many messages to read (50)")] [MaxValue(200)] int read = 50)
    {
        //Initial response
        var embed = new EmbedBuilder()
            .WithColor(Color.Blue)
            .WithTitle("Chat Analysis")
            .WithDescription("Processing")
            .WithFooter("Powered by OpenAI GPT-4");
        await RespondAsync(embeds: new[] { embed.Build() }, ephemeral: true);
        //Get values
        var channel = Context.Channel as ITextChannel;
        var messages = await channel.GetMessagesAsync(200).FlattenAsync();
        //Filter messages to ignore, select number to read, and order by date
        var messageString = messages.Where(m =>
                !string.IsNullOrWhiteSpace(m.Content) &&
                !m.Author.IsBot
            ).Take(read).OrderBy(m => m.CreatedAt)
            .Aggregate("", (current, message) => current + $"{message.Author.Username}: {message.CleanContent}\n");
        //Send to API
        try
        {
            var client = new BingChatClient(new BingChatClientOptions
            {
                CookieU = "null",
                Tone = BingChatTone.Creative,
            });
            var chatMessage =
@"Act as a discord assistant by the name of Wahaha, analyze the conversation and provide a summary of its topic and any rule violations. Suggest an action based on these violations. Consider these rules:
1) Follow Discord's Terms of Service and Community Guidelines.
2) Obey Moderation and Common Sense; disputes should be made in a ticket.
3) No Offensive Speech or harassment.
4) Be Kind; discourteous messages will be removed.
5) No Spam or Shitposting; follow channel rules.
6) English-Only conversations.
7) No Alt Accounts or Impersonation.
8) No Political Discussion; keep it light-hearted.
9) No Sexual Topics; occasional mature jokes allowed.
10) No Extremely Distressing topics; seek professional help for mental health issues.";
            chatMessage += $"\n\n{messageString}";
            var response = await client.AskAsync(chatMessage);
            embed.WithDescription(response);
            await ModifyOriginalResponseAsync(m => m.Embeds = new[] { embed.Build() });
        }
        catch (Exception e)
        {
            var response = "Failed to analyse chat: " + e.Message;
            embed.WithDescription(response);
            await ModifyOriginalResponseAsync(m => m.Embeds = new[] { embed.Build() });
        }
    }
}