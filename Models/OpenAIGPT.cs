using Azure.AI.OpenAI;
using Azure;
using Discord;
using DougBot.Systems.EventBased;

namespace DougBot.Models
{

    public class OpenAIGPT
    {
        public static async Task<string> Wah354k(string systemPrompt, string message)
        {
            //Setup OpenAI
            var client = new OpenAIClient(new Uri(Environment.GetEnvironmentVariable("AI_URL")), new AzureKeyCredential(Environment.GetEnvironmentVariable("AI_TOKEN")));
            var chatCompletionsOptions = new ChatCompletionsOptions
            {
                MaxTokens = 2000,
                Temperature = 0.5f,
                PresencePenalty = 0.5f,
                FrequencyPenalty = 0.5f
            };
            chatCompletionsOptions.StopSequences.Add("\n");
            //Add messages to chat
            chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.System, systemPrompt));
            chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.User, message));
            var completionResponse = await client.GetChatCompletionsAsync("Wah-35-4k", chatCompletionsOptions);
            var chatCompletions = completionResponse.Value;
            //Log tokens used and price
            var fields = new List<EmbedFieldBuilder>{
                new ()
                {
                    Name = "Input",
                    Value = $"System: {systemPrompt}\n" +
                            $"User: {message}",
                    IsInline = false
                },
                new ()
                {
                    Name = "Output",
                    Value = chatCompletions.Choices[0].Message.Content,
                    IsInline = false
                },
                new()
                {
                    Name = "Tokens",
                    Value = $"Prompt: {chatCompletions.Usage.PromptTokens}\n" +
                            $"Completion: {chatCompletions.Usage.CompletionTokens}\n" +
                            $"Total: {chatCompletions.Usage.TotalTokens}\n",
                    IsInline = false
                }
            };
            //Send audit log
            await AuditLog.LogEvent("OpenAI: Wah-35-4k", "290611616586924033", Color.Green, fields);
            return chatCompletions.Choices[0].Message.Content;
        }
    }
}
