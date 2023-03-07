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
    public async Task AIChat([Summary(description: "Input (e.g. Where is the capital of Germany?)")] string input,
        [Summary(description: "Prompt for the AI (e.g. You are a helpful assistant)")]
        string prompt = "You are a helpful and informative assistant")
    {
        await RespondAsync("Command received", ephemeral: true);
        var dbGuild = await Guild.GetGuild(Context.Guild.Id.ToString());
        //if full override is set then just send that, else get chat
        var openAiService = new OpenAIService(new OpenAiOptions
        {
            ApiKey = dbGuild.OpenAiToken
        });
        var completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
        {
            Messages = new List<ChatMessage>
            {
                ChatMessage.FromSystem(prompt),
                ChatMessage.FromUser(input)
            },
            Model = OpenAI.GPT3.ObjectModels.Models.ChatGpt3_5Turbo
        });
        if (!completionResult.Successful) throw new Exception("API Error: " + completionResult.Error);
        var aiText = completionResult.Choices.First().Message.Content;
        if (string.IsNullOrWhiteSpace(aiText))
        {
            await ModifyOriginalResponseAsync(r => r.Content = "No response from API");
            return;
        }

        var builder = new ComponentBuilder()
            .WithButton("Send to chat", "aiChatApprove");
        await ModifyOriginalResponseAsync(r => r.Content = aiText);
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