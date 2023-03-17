using System.Net.Http.Headers;
using System.Text.Json;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DougBot.Models;
using DougBot.Systems;

namespace DougBot.SlashCommands;

public class AIChatCmd : InteractionModuleBase
{
    [SlashCommand("aichat", "Send an AI message into chat")]
    [EnabledInDm(false)]
    [DefaultMemberPermissions(GuildPermission.Administrator)]
    public async Task AIChat( [Summary("read", "How many messages to read")] int read = 20)
    {
        await RespondAsync("Command received", ephemeral: true);
        var dbGuild = await Guild.GetGuild(Context.Guild.Id.ToString());
        //Get chat messages
        var messages = await Context.Channel.GetMessagesAsync(read).FlattenAsync();
        messages = messages.Where(m =>
            !string.IsNullOrWhiteSpace(m.Content) &&
            (m.Flags.Value & MessageFlags.Ephemeral) == 0 &&
            !dbGuild.OpenAiUserBlacklist.Contains(m.Author.Id.ToString())
            ).OrderBy(m => m.CreatedAt);
        const string prompt = "You are a chat bot named Wah in a discord server of many people.\n" +
                              "Take in the text from the chat and produce a response to one of the users.\n" +
                              "You must always respond in a slightly annoyed and sarcastic manner\n" +
                              "When responding to a specific person ensure you say their name\n" +
                              "You can not make appointments, do not even mention them\n" +
                              "Your pronouns are application/json\n" +
                              "Do not say anything rude\n" +
                              "Do not say anything offensive\n" +
                              "Do not say anything mean\n\n";
        //Process all messages
        var messageString = "";
        var botUser = Context.Client.CurrentUser;
        foreach (var message in messages)
        {
            //If message is a reply check if it was directed to Wah, if not then discard
            if (message.Reference != null)
            {
                var replyID = message.Reference.MessageId;
                var replyMessage = await Context.Channel.GetMessageAsync((ulong)replyID);
                if (replyMessage != null && replyMessage.Author.Id == botUser.Id)
                    messageString += $"{message.Author.Username}: Wah, {message.CleanContent}\n";
            }
            else
            {
                messageString += $"{message.Author.Username}: {message.CleanContent}\n";
            }
        }
        messageString = messageString.Replace($"@{botUser.Username}#{botUser.Discriminator}", "Wah,")
            .Replace($"WAHAHA: Command received", "Wah")
            .Replace($"@", "");
        messageString += "Wah:";
        //Send to API
        using var client = new HttpClient();
        var data = new
        {
            prompt = prompt + messageString,
            max_tokens = 500,
            temperature = 0.9,
            frequency_penalty = 0,
            presence_penalty = 0,
            top_p = 1,
            best_of = 1,
            stop = new[] { "\n", "Wah:" }
        };
        var content = new StringContent(JsonSerializer.Serialize(data));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        content.Headers.Add("api-key", dbGuild.OpenAiToken);

        var response = await client.PostAsync(dbGuild.OpenAiURL, content);
        var responseContent = await response.Content.ReadAsStringAsync();
        responseContent = responseContent.Replace("Wah:", "");
        var json = JsonDocument.Parse(responseContent);
        var text = json.RootElement.GetProperty("choices")[0].GetProperty("text").GetString();
        //Respond
        if (string.IsNullOrWhiteSpace(text))
        {
            await ModifyOriginalResponseAsync(r => r.Content = "No response from API");
            return;
        }
        var builder = new ComponentBuilder()
            .WithButton("Send to chat", "aiChatApprove");
        await ModifyOriginalResponseAsync(r => r.Content = text);
        await ModifyOriginalResponseAsync(m => m.Components = builder.Build());
    }

    [ComponentInteraction("aiChatApprove")]
    public async Task ApproveResponse()
    {
        var interaction = Context.Interaction as SocketMessageComponent;
        await RespondAsync("Approved, Typing and sending", ephemeral: true);
        await Context.Channel.TriggerTypingAsync();
        await Task.Delay(3000);
        var response = await ReplyAsync(interaction.Message.Content);
        var auditFields = new List<EmbedFieldBuilder>
        {
            new()
            {
                Name = "Approved By",
                Value = Context.User.Mention,
                IsInline = true
            },
            new()
            {
                Name = "Channel",
                Value = (Context.Channel as SocketTextChannel).Mention,
                IsInline = true
            },
            new()
            {
                Name = "Message",
                Value = $"[{interaction.Message.Content}]({response.GetJumpUrl()})",
                IsInline = false
            }
        };
        AuditLog.LogEvent("***AI Message Approved***", Context.Guild.Id.ToString(), Color.Green, auditFields);
    }
}