using System.Text.RegularExpressions;
using Discord;
using Discord.Interactions;
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
        var settings = Setting.GetSettings();
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
        var aiText = SanitizeString(completionResult.Choices.FirstOrDefault().Text);
        var cost = (decimal)(completionResult.Usage.TotalTokens * 0.000002);
        await ModifyOriginalResponseAsync(r => r.Content = "Response received, moderating content");
        //check the response is not offensive using the OpenAI moderation API
        var moderationResult = await openAiService.Moderation.CreateModeration(new CreateModerationRequest
        {
            Input = aiText
        });
        if (!moderationResult.Successful) throw new Exception("API Error: " + moderationResult.Error);
        //Moderation result output string
        var categoryscores = moderationResult.Results[0].CategoryScores;
        var moderationFlagged = (decimal)categoryscores.Hate > (decimal)0.005 ||
                                (decimal)categoryscores.HateThreatening > (decimal)0.005 ||
                                (decimal)categoryscores.Selfharm > (decimal)0.005 ||
                                (decimal)categoryscores.Sexual > (decimal)0.005 ||
                                (decimal)categoryscores.SexualMinors > (decimal)0.005 ||
                                (decimal)categoryscores.Violence > (decimal)0.01 ||
                                (decimal)categoryscores.Violencegraphic > (decimal)0.005;
        var blacklistFlagged = settings.OpenAiWordBlacklist.ToLower().Split(",").Any(s => aiText.ToLower().Contains(s));
        //Respond
        await ModifyOriginalResponseAsync(r => r.Content = "Content moderated, processing response");
        if (!moderationFlagged && !string.IsNullOrWhiteSpace(aiText) && !blacklistFlagged)
        {
            Context.Channel.TriggerTypingAsync();
            await Task.Delay(5000);
            await ReplyAsync(aiText);
            await ModifyOriginalResponseAsync(r => r.Content = "Moderation success, message sent");
        }
        else
        {
            var builder = new ComponentBuilder()
                .WithButton("Override Filter", "aiChatApprove", ButtonStyle.Danger);
            await ModifyOriginalResponseAsync(r => r.Content =
                "Moderation fail, please confirm you want to send this message\n" +
                "Response: " + aiText);
            await ModifyOriginalResponseAsync(m => m.Components = builder.Build());
        }

        //Log
        var auditFields = new List<EmbedFieldBuilder>
        {
            new()
            {
                Name = "Tokens",
                Value = completionResult.Usage.TotalTokens,
                IsInline = true
            },
            new()
            {
                Name = "Cost",
                Value = cost,
                IsInline = true
            },
            string.IsNullOrEmpty(aiText)
                ? new EmbedFieldBuilder
                {
                    Name = "Response",
                    Value = aiText,
                    IsInline = true
                }
                : new EmbedFieldBuilder
                {
                    Name = "Response",
                    Value = "No response",
                    IsInline = true
                },
            new()
            {
                Name = "Filter Result",
                Value = "If either of the filters were triggered",
                IsInline = false
            },
            new()
            {
                Name = "Blacklist Flagged",
                Value = blacklistFlagged,
                IsInline = true
            },
            new()
            {
                Name = "Moderation Flagged",
                Value = moderationFlagged,
                IsInline = true
            },
            new()
            {
                Name = "Filter Values",
                Value = "The values of all the categories as rated by the AI",
                IsInline = false
            },
            new()
            {
                Name = "Hate",
                Value = (decimal)categoryscores.Hate,
                IsInline = true
            },
            new()
            {
                Name = "Hate Threatening",
                Value = (decimal)categoryscores.HateThreatening,
                IsInline = true
            },
            new()
            {
                Name = "Self Harm",
                Value = (decimal)categoryscores.Selfharm,
                IsInline = true
            },
            new()
            {
                Name = "Sexual",
                Value = (decimal)categoryscores.Sexual,
                IsInline = true
            },
            new()
            {
                Name = "Sexual Minors",
                Value = (decimal)categoryscores.SexualMinors,
                IsInline = true
            },
            new()
            {
                Name = "Violence",
                Value = (decimal)categoryscores.Violence,
                IsInline = true
            },
            new()
            {
                Name = "Violence Graphic",
                Value = (decimal)categoryscores.Violencegraphic,
                IsInline = true
            }
        };
        AuditLog.LogEvent("***Message Processed***",
            !(moderationFlagged || blacklistFlagged), auditFields);
    }

    private static string SanitizeString(string str)
    {
        return Regex.Replace(str, "[^a-zA-Z0-9 ,?'`.\"]", "", RegexOptions.Compiled);
    }
}