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
    public async Task AIChat([Summary(description: "Prompt for the AI")] string prompt)
    {
        await RespondAsync("Command received", ephemeral: true);
        var dbGuild = await Guild.GetGuild(Context.Guild.Id.ToString());
        //if full override is set then just send that, else get chat
        var openAiService = new OpenAIService(new OpenAiOptions
        {
            ApiKey = dbGuild.OpenAiToken
        });
        var completionResult = await openAiService.Completions.CreateCompletion(new CompletionCreateRequest
        {
            Prompt = prompt,
            MaxTokens = 250,
            Temperature = (float)0.6,
            TopP = 1,
            PresencePenalty = (float)0.3,
            FrequencyPenalty = (float)0.3
        }, OpenAI.GPT3.ObjectModels.Models.Davinci);
        if (!completionResult.Successful) throw new Exception("API Error: " + completionResult.Error);
        var aiText = completionResult.Choices.FirstOrDefault().Text;
        if (string.IsNullOrWhiteSpace(aiText))
        {
            await ModifyOriginalResponseAsync(r => r.Content = "No response from API");
            return;
        }
        var builder = new ComponentBuilder()
            .WithButton("Send to chat", "aiChatApprove")
            .WithButton("Retry", "aiChatRetry", ButtonStyle.Secondary);
        await ModifyOriginalResponseAsync(r => r.Content =
            "Message ready, Please approve\n" +
            "Response: " + aiText);
        await ModifyOriginalResponseAsync(m => m.Components = builder.Build());
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
        AuditLog.LogEvent("***AI Message Approved***", Context.Guild.Id.ToString(), Color.Green, auditFields);
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
        AuditLog.LogEvent("***AI Message Declined***", Context.Guild.Id.ToString(), Color.Red, auditFields);
    }
}