using Discord;
using DougBot.Systems.EventBased;
using OpenAI.Managers;
using OpenAI;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.ObjectModels.ResponseModels;

namespace DougBot.Models
{

    public class OpenAIGPT
    {
        public static async Task<ChatCompletionCreateResponse> Wah354k(List<ChatMessage> chatMessages)
        {
            try
            {
                //Setup OpenAI
                var openAiService = new OpenAIService(new OpenAiOptions()
                {
                    BaseDomain = Environment.GetEnvironmentVariable("AI_URL"),
                    ApiKey = Environment.GetEnvironmentVariable("AI_TOKEN"),
                    DeploymentId = "Wah-35-4k",
                    ResourceName = "dougAI",
                    ProviderType = ProviderType.Azure
                });
                var completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
                {
                    Messages = chatMessages,
                    Model = OpenAI.ObjectModels.Models.ChatGpt3_5Turbo,
                    MaxTokens = 500,
                    Temperature = 0.9f,
                    PresencePenalty = 0.6f,
                    FrequencyPenalty = 0.0f,
                    StopAsList = new List<string> { "\n" }
                });
                var chatText = completionResult.Choices.First().Message.Content;
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
                        Value = $"Prompt: {completionResult.Usage.PromptTokens}\n" +
                                $"Completion: {completionResult.Usage.CompletionTokens}\n" +
                                $"Total: {completionResult.Usage.TotalTokens}\n",
                        IsInline = false
                    }
                };
                //Send audit log
                await AuditLog.LogEvent("OpenAI: Wah-35-4k", "290611616586924033", Color.Green, fields);
                return completionResult;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[General/Warning] {DateTime.UtcNow:HH:mm:ss} OpenAIGPT {e}");
                throw;
            }
        }
    }
}
