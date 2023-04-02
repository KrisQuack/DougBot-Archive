using Azure;
using Azure.AI.OpenAI;
using Discord;
using Discord.Interactions;
using Discord.WebSocket;
using DougBot.Models;
using DougBot.Systems;

namespace DougBot.SlashCommands;
[Group("ai", "AI based commands")]
public class AIChatCmd : InteractionModuleBase
{

    [SlashCommand("analyze", "Analyze the current chat")]
    [EnabledInDm(false)]
    [DefaultMemberPermissions(GuildPermission.ModerateMembers)]
    public async Task Analyze([Summary("read", "How many messages to read (50)"), MaxValue(200)] int read = 50)
    {
        await RespondAsync("Analyzing...", ephemeral: true);
        var dbGuild = await Guild.GetGuild(Context.Guild.Id.ToString());
        var messages = await Context.Channel.GetMessagesAsync(200).FlattenAsync();
        //Filter messages to ignore, select number to read, and order by date
        var messageString = messages.Where(m =>
            !string.IsNullOrWhiteSpace(m.Content) &&
            !m.Author.IsBot
            ).Take(read).OrderBy(m => m.CreatedAt)
            .Aggregate("", (current, message) => current + $"{message.Author.Username}: {message.CleanContent}\n");
        //Send to API
        var client = new OpenAIClient(new Uri(dbGuild.OpenAiURL), new AzureKeyCredential(dbGuild.OpenAiToken));
        try
        {
            Response<Completions> response = await client.GetCompletionsAsync("WahSpeech", new CompletionsOptions
            {
                Prompts =
                {
                    string.Join("\n",dbGuild.OpenAiPrompt) + messageString + "\n\nAssistant:"
                },
                MaxTokens = 100,
                Temperature = 0.5f,
                PresencePenalty = 0.5f,
                FrequencyPenalty = 0.5f,
                StopSequences = { "Assistant:", "\n" }
            });
            var completion = response.Value.Choices[0].Text;
            await ModifyOriginalResponseAsync(m => m.Content = completion);
        }
        catch (Exception e)
        {
            await ModifyOriginalResponseAsync(m => m.Content = "Failed to analyze chat: "+e.Message);
        }
    }
}