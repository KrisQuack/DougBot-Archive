using Azure.AI.OpenAI;
using Azure;
using Discord;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace DougBot.Models
{

    public class OpenAIGPT
    {
        public static async Task<string> Wah354k (string systemPrompt, string message)
        {
            //Setup OpenAI
            var client = new OpenAIClient(new Uri(Environment.GetEnvironmentVariable("AI_URL")),new AzureKeyCredential(Environment.GetEnvironmentVariable("AI_TOKEN")));
            var chatCompletionsOptions = new ChatCompletionsOptions
            {
                MaxTokens = 2000,
                Temperature = 0.5f,
                PresencePenalty = 0.5f,
                FrequencyPenalty = 0.5f
            };
            chatCompletionsOptions.StopSequences.Add("\n");
            chatCompletionsOptions.StopSequences.Add("User:");
            chatCompletionsOptions.StopSequences.Add("Assistant:");
            //Add messages to chat
            chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.System, systemPrompt));
            chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.User, message));
            var completionResponse = await client.GetChatCompletionsAsync("Wah-35-4k", chatCompletionsOptions);
            var chatCompletions = completionResponse.Value;
            return chatCompletions.Choices[0].Message.Content;
        }
    }
}
