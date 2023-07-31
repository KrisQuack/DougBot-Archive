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
        public static async Task<string> Wah354k(List<ChatMessage> chatMessages)
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
                var chatText = completionResult.Choices?.FirstOrDefault()?.Message.Content ?? "No";
                return chatText;
            }
            catch (Exception e)
            {
                Console.WriteLine($"[General/Warning] {DateTime.UtcNow:HH:mm:ss} OpenAIGPT {e}");
                throw;
            }
        }
    }
}
