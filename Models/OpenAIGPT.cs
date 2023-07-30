using Discord;
using DougBot.Systems.EventBased;
using OpenAI.Managers;
using OpenAI;
using OpenAI.ObjectModels.RequestModels;

namespace DougBot.Models
{

    public class OpenAIGPT
    {
        public static async Task<string> Wah354k(string systemPrompt, string message)
        {
            try
            {
                //Setup OpenAI
                var openAiService = new OpenAIService(new OpenAiOptions()
                {
                    ApiKey = Environment.GetEnvironmentVariable("AI_TOKEN")
                });
                var completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
                {
                    Messages = new List<ChatMessage>
                    {
                        ChatMessage.FromSystem(systemPrompt),
                        ChatMessage.FromUser(message)
                    },
                    Model = OpenAI.ObjectModels.Models.ChatGpt3_5Turbo,
                    MaxTokens = 500,
                    Temperature = 0.9f,
                    PresencePenalty = 0.6f,
                    FrequencyPenalty = 0.0f,
                    StopAsList = new List<string> { "\n" }
                });
                var chatCompletions = completionResult;
                var chatText = chatCompletions.Choices.First().Message.Content;
                chatText = string.IsNullOrEmpty(chatText) ? "No" : chatText;
                //Log tokens used and price
                var fields = new List<EmbedFieldBuilder>{
                new ()
                {
                    Name = "Output",
                    Value = chatText,
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
                return chatText;
            }
            catch (Exception e)
            {
                if (e.Message.Contains("content management policy")) return "Blocked by OpenAI Policy";
                throw;
            }
        }
    }
}
