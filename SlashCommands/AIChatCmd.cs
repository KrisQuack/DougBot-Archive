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
            "The following is a conversation with an AI assistant named Wah. The assistant is helpful, creative, clever, and very friendly.")
    {
        await RespondAsync("Command received", ephemeral: true);
        var typingObj = Context.Channel.EnterTypingState();
        var settings = Setting.GetSettings();
        //Get chat to send
        var messages = await Context.Channel.GetMessagesAsync(procCount).FlattenAsync();
        var queryString = pretext + "\n\n";
        //Ignore embeds and media
        messages = messages.Where(m =>
            m.Embeds.Count == 0 &&
            m.Attachments.Count == 0 &&
            !m.Content.StartsWith("<") && !m.Content.EndsWith(">")
        ).OrderBy(m => m.Timestamp);
        //Process all messages
        foreach (var message in messages)
            if (message.Author.Id == Context.Client.CurrentUser.Id)
                queryString += $"Wah: {message.Content}\n";
            else
                queryString += $"{message.Author.Username}: {message.Content}\n";

        queryString += "Wah: ";
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
            Temperature = (float)0.9,
            TopP = 1,
            PresencePenalty = (float)0.3,
            FrequencyPenalty = (float)0.3,
            Stop = "\n"
        }, OpenAI.GPT3.ObjectModels.Models.Davinci);
        if (!completionResult.Successful) throw new Exception("API Error: " + completionResult.Error);
        var aiText = completionResult.Choices.FirstOrDefault().Text;
        var cost = (decimal)(completionResult.Usage.TotalTokens * 0.000002);
        await ModifyOriginalResponseAsync(r => r.Content = "Response received, moderating content");
        //check the response is not offensive using the OpenAI moderation API
        var moderationResult = await openAiService.Moderation.CreateModeration(new CreateModerationRequest
        {
            Input = aiText
        });
        if (!moderationResult.Successful) throw new Exception("API Error: " + moderationResult.Error);
        //Moderation result output string
        var categories = moderationResult.Results[0].Categories;
        var categoryscores = moderationResult.Results[0].CategoryScores;
        var moderationString = $"**Hate:** {categories.Hate} {(decimal)categoryscores.Hate}\n" +
                               $"**Hate Threatening:** {categories.HateThreatening} {(decimal)categoryscores.HateThreatening}\n" +
                               $"**Self Harm:** {categories.Selfharm} {(decimal)categoryscores.Selfharm}\n" +
                               $"**Sexual:** {categories.Sexual} {(decimal)categoryscores.Sexual}\n" +
                               $"**Sexual Minors:** {categories.Sexualminors} {(decimal)categoryscores.SexualMinors}\n" +
                               $"**Violence:** {categories.Violence} {(decimal)categoryscores.Violence}\n" +
                               $"**Violence Graphic:** {categories.Violencegraphic} {(decimal)categoryscores.Violencegraphic}\n";
        var moderationFlagged = (decimal)categoryscores.Hate > (decimal)0.001 ||
                                (decimal)categoryscores.HateThreatening > (decimal)0.001 ||
                                (decimal)categoryscores.Selfharm > (decimal)0.001 ||
                                (decimal)categoryscores.Sexual > (decimal)0.001 ||
                                (decimal)categoryscores.SexualMinors > (decimal)0.001 ||
                                (decimal)categoryscores.Violence > (decimal)0.001 ||
                                (decimal)categoryscores.Violencegraphic > (decimal)0.001;
        var blacklistFlagged = settings.OpenAiWordBlacklist.ToLower().Split(",").Any(s => aiText.ToLower().Contains(s));
        //Respond
        var resultEmbed = await AuditLog.LogEvent(Context.Client as DiscordSocketClient,
            "***Message Processed***\n" +
            $"**Tokens:** Input {completionResult.Usage.PromptTokens} + Output {completionResult.Usage.CompletionTokens} = {completionResult.Usage.TotalTokens}\n" +
            $"**Cost:** ${cost}\n" +
            $"**Response:** {aiText}\n" +
            $"**Blacklist Flagged:** {blacklistFlagged}\n" +
            $"**Moderation Flagged:** {moderationFlagged}\n" + moderationString,
            !(moderationFlagged || blacklistFlagged));
        await ModifyOriginalResponseAsync(r => r.Content = "Content moderated, processing response");
        await ModifyOriginalResponseAsync(m => m.Embed = resultEmbed.Build());
        if (!moderationFlagged && aiText != "" && !blacklistFlagged)
            //Send message
            await ReplyAsync(aiText);

        typingObj.Dispose();
    }
}