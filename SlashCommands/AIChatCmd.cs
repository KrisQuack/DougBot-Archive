using System.Text.Json;
using Discord;
using Discord.Interactions;
using DougBot.Models;
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
        [Summary(description: "Messages to process (Default: 15)")] [MaxValue(500)] int procCount = 15,
        [Summary(description: "Pretext given to the bot")]
        string pretext = "")
    {
        await RespondAsync("Command received", ephemeral: true);
        var settings = Setting.GetSettings();
        //Get chat to send
        var messages = await Context.Channel.GetMessagesAsync(procCount).FlattenAsync();
        var queryString = pretext + "\n";
        //Ignore embeds and media
        messages = messages.Where(m =>
            m.Embeds.Count == 0 &&
            m.Attachments.Count == 0 &&
            !m.Content.StartsWith("<") && !m.Content.EndsWith(">")
        ).OrderBy(m => m.Timestamp);
        //Process all messages
        foreach (var message in messages) queryString += $"Friend: {message.Content}\n";
        queryString += "You: ";
        await ModifyOriginalResponseAsync(r => r.Content += "Messages loaded, Querying APi");
        //Query API for chat response
        var openAiService = new OpenAIService(new OpenAiOptions
        {
            ApiKey = settings.OpenAiToken
        });
        var completionResult = await openAiService.Completions.CreateCompletion(new CompletionCreateRequest
        {
            Prompt = queryString,
            MaxTokens = 200,
            Temperature = (float)0.5,
            TopP = 1,
            PresencePenalty = (float)0.0,
            FrequencyPenalty = (float)0.5,
            Stop = "\n"
        }, OpenAI.GPT3.ObjectModels.Models.Curie);
        if (!completionResult.Successful) throw new Exception("API Error: " + completionResult.Error);
        var aiText = completionResult.Choices.FirstOrDefault().Text;
        await ModifyOriginalResponseAsync(r => r.Content = "Response received, moderating content");
        //check the response is not offensive using the OpenAI moderation API
        var moderationResult = await openAiService.Moderation.CreateModeration(new CreateModerationRequest
        {
            Input = aiText
        });
        //Moderation result output string
        var categories = moderationResult.Results[0].Categories;
        var categoryscores = moderationResult.Results[0].CategoryScores;
        var moderationString = $"**Hate:** {categories.Hate} {(decimal)categoryscores.Hate}\n"+
                               $"**Hate Threatening:** {categories.HateThreatening} {(decimal)categoryscores.HateThreatening}\n"+
                               $"**Self Harm:** {categories.Selfharm} {(decimal)categoryscores.Selfharm}\n"+
                               $"**Sexual:** {categories.Sexual} {(decimal)categoryscores.Sexual}\n"+
                               $"**Sexual Minors:** {categories.Sexualminors} {(decimal)categoryscores.SexualMinors}\n"+
                               $"**Violence:** {categories.Violence} {(decimal)categoryscores.Violence}\n"+
                               $"**Violence Graphic:** {categories.Violencegraphic} {(decimal)categoryscores.Violencegraphic}\n";
        if (!moderationResult.Successful) throw new Exception("API Error: " + moderationResult.Error);
        await ModifyOriginalResponseAsync(r => r.Content = "Moderation complete");
        //calculate cost
        var cost = (decimal)(completionResult.Usage.TotalTokens * 0.000002);
        //Respond
        var blacklist = settings.OpenAiWordBlacklist.Split(",");
        if (moderationResult.Results.Any(r => !r.Flagged) && aiText != "" && !blacklist.Any(s=>aiText.Contains(s)))
        {
            await ReplyAsync(aiText);
            await ModifyOriginalResponseAsync(m =>
                m.Content = "***Message Sent***\n" +
                             $"**Tokens:** Input {completionResult.Usage.PromptTokens} + Output {completionResult.Usage.CompletionTokens} = {completionResult.Usage.TotalTokens}\n" +
                             $"**Cost:** ${cost}\n" +
                             $"**Response:** {aiText}\n" +
                             $"**Moderation Result:** Pass\n"+moderationString);
        }
        else
        {
            await ModifyOriginalResponseAsync(m =>
                m.Content = "***Message failed to pass content moderation***\n" +
                             $"**Tokens:** Input {completionResult.Usage.PromptTokens} + Output {completionResult.Usage.CompletionTokens} = {completionResult.Usage.TotalTokens}\n" +
                             $"**Cost:** ${cost}\n" +
                             $"**Response:** {aiText}\n"+
                             $"**Moderation Result:** Fail\n"+moderationString);
        }
    }
}