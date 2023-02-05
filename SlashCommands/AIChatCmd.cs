using System.Text.RegularExpressions;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DougBot.Models;
using DougBot.Systems;
using OpenAI.GPT3;
using OpenAI.GPT3.Managers;
using OpenAI.GPT3.ObjectModels.RequestModels;

namespace DougBot.SlashCommands;

public class AIChatCmd : InteractionModuleBase
{
    [SlashCommand("aichat", "Send an AI message into chat")]
    [EnabledInDm(false)]
    [DefaultMemberPermissions(GuildPermission.Administrator)]
    public async Task AIChat(
        [Summary(description: "Messages to process (Default: 15)")] [MaxValue(500)]
        int procCount = 15,
        [Summary(description: "Pretext given to the bot")]
        string pretext =
            "The following is a conversation with an english speaking AI assistant named Wah. The assistant is helpful, creative, clever, and very friendly.")
    {
        await RespondAsync("Command received", ephemeral: true);
        var dbGuild = await Guild.GetGuild(Context.Guild.Id.ToString());
        //Get chat to send
        var messages = await Context.Channel.GetMessagesAsync(procCount).FlattenAsync();
        var queryString = pretext + "\n\n";
        //Ignore embeds and media
        messages = messages.Where(m =>
            m.Embeds.Count == 0 &&
            m.Attachments.Count == 0
        ).OrderBy(m => m.Timestamp);
        //Process all messages
        var botUser = Context.Client.CurrentUser;
        foreach (var message in messages)
        {
            var messageClean = SanitizeString(message.CleanContent);
            if (message.Author.Id == botUser.Id)
            {
                queryString += $"Wah: {messageClean}\n";
            }
            else
            {
                //If message is a reply check if it was directed to Wah, if not then discard
                if (message.Reference != null)
                {
                    var replyID = message.Reference.MessageId;
                    var replyMessage = await Context.Channel.GetMessageAsync((ulong)replyID);
                    if (replyMessage != null && replyMessage.Author.Id == botUser.Id)
                        queryString += $"{SanitizeString(message.Author.Username)}: Wah, {messageClean}\n";
                }
                else
                {
                    queryString += $"{SanitizeString(message.Author.Username)}: {messageClean}\n";
                }
            }
        }

        queryString = queryString.Replace($"@{botUser.Username}#{botUser.Discriminator}", "Wah");
        queryString += "Wah: ";
        await ModifyOriginalResponseAsync(r => r.Content += "Messages loaded, Querying APi");
        //Query API for chat response
        var openAiService = new OpenAIService(new OpenAiOptions
        {
            ApiKey = dbGuild.OpenAiToken
        });
        var completionResult = await openAiService.Completions.CreateCompletion(new CompletionCreateRequest
        {
            Prompt = queryString,
            MaxTokens = 500,
            Temperature = (float)0.9,
            TopP = 1,
            PresencePenalty = (float)0.3,
            FrequencyPenalty = (float)0.3,
            Stop = "\n"
        }, OpenAI.GPT3.ObjectModels.Models.Davinci);
        if (!completionResult.Successful) throw new Exception("API Error: " + completionResult.Error);
        var aiText = completionResult.Choices.FirstOrDefault().Text;
        if (!string.IsNullOrWhiteSpace(aiText))
        {
            var builder = new ComponentBuilder()
                .WithButton("Approve", "aiChatApprove", ButtonStyle.Success)
                .WithButton("Decline", "aiChatDecline", ButtonStyle.Danger);
            await ModifyOriginalResponseAsync(r => r.Content =
                "Message ready, Please approve\n" +
                "Response: " + aiText);
            await ModifyOriginalResponseAsync(m => m.Components = builder.Build());
        }
        else
        {
            await ModifyOriginalResponseAsync(r => r.Content = "No response from API");
        }

        await Task.Delay(10000);
    }

    private static string SanitizeString(string str)
    {
        return Regex.Replace(str, "[^a-zA-Z0-9 ,?'`.\"]", "", RegexOptions.Compiled);
    }

    [ComponentInteraction("aiChatApprove")]
    public async Task ApproveResponse()
    {
        var interaction = Context.Interaction as SocketMessageComponent;
        var message = interaction.Message.Content.Split("\n")[1].Replace("Response: ", "");
        await RespondAsync("Approved, Typing and sending", ephemeral: true);
        await Context.Channel.TriggerTypingAsync();
        await Task.Delay(3000);
        var response = await ReplyAsync(message);
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
                Value = $"[{message}]({response.GetJumpUrl()})",
                IsInline = false
            }
        };
        AuditLog.LogEvent("***AI Message Approved***", Context.Guild.Id.ToString(), true, auditFields);
    }

    [ComponentInteraction("aiChatDecline")]
    public async Task DeclineResponse()
    {
        var interaction = Context.Interaction as SocketMessageComponent;
        var message = interaction.Message.Content.Split("\n")[1].Replace("Response: ", "");
        await RespondAsync("Declined", ephemeral: true);
        var auditFields = new List<EmbedFieldBuilder>
        {
            new()
            {
                Name = "Declined By",
                Value = Context.User.Mention,
                IsInline = true
            },
            new()
            {
                Name = "Message",
                Value = message,
                IsInline = true
            }
        };
        AuditLog.LogEvent("***AI Message Declined***", Context.Guild.Id.ToString(), false, auditFields);
    }
}