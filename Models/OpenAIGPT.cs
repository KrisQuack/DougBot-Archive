using OpenAI;
using OpenAI.Managers;
using OpenAI.ObjectModels.RequestModels;
using OpenAI.Tokenizer.GPT3;

namespace DougBot.Models;

public class OpenAIGPT
{
    public static async Task<string> Gpt354k(List<ChatMessage> chatMessages)
    {
        try
        {
            return await GetChatCompletion("gpt-35-4k", chatMessages, 4000);
        }
        catch (Exception)
        {
            return await Gpt3516k(chatMessages);
        }
    }

    public static async Task<string> Gpt3516k(List<ChatMessage> chatMessages)
    {
        return await GetChatCompletion("gpt-35-16k", chatMessages, 16000);
    }

    public static async Task<string> Gpt48k(List<ChatMessage> chatMessages)
    {
        try
        {
            return await GetChatCompletion("gpt-4-8k", chatMessages, 8000);
        }
        catch (Exception)
        {
            return await Gpt432k(chatMessages);
        }
    }

    public static async Task<string> Gpt432k(List<ChatMessage> chatMessages)
    {
        return await GetChatCompletion("gpt-4-32k", chatMessages, 32000);
    }

    private static async Task<string> GetChatCompletion(string deploymentId, List<ChatMessage> chatMessages,
        int tokenLimit)
    {
        var maxTokens = 1000;
        //Estimate tokens
        var tokens = TokenizerGpt3.TokenCount(string.Join("\n", chatMessages.Select(m => m.Content)), true) + maxTokens;
        if (tokens > tokenLimit) throw new Exception("Too many tokens");
        var openAiService = new OpenAIService(new OpenAiOptions
        {
            BaseDomain = ConfigurationService.Instance.AiUrl,
            ApiKey = ConfigurationService.Instance.AiToken,
            DeploymentId = deploymentId,
            ResourceName = "dougAI",
            ProviderType = ProviderType.Azure
        });
        var completionResult = await openAiService.ChatCompletion.CreateCompletion(new ChatCompletionCreateRequest
        {
            Messages = chatMessages,
            MaxTokens = maxTokens,
            Model = deploymentId,
            Temperature = 0.9f,
            PresencePenalty = 0.6f,
            FrequencyPenalty = 0.0f,
            StopAsList = new List<string> { "\n" }
        });
        return completionResult.Choices?.FirstOrDefault()?.Message.Content ?? "No";
    }
}