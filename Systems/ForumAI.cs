using Azure;
using Azure.AI.OpenAI;
using Discord;
using Discord.WebSocket;
using DougBot.Models;
using Microsoft.Extensions.Azure;

namespace DougBot.Systems;

public static class ForumAi
{

    public static async Task Monitor()
    {
        var client = Program._Client;
        client.MessageReceived += ForumAiHandler;
    }

    private static Task ForumAiHandler(SocketMessage arg)
    {
        _ = Task.Run(async () =>
        {
            if(!arg.Author.IsBot && arg.Channel.GetType() == typeof(SocketThreadChannel))
            {
                var threadMessage = arg as SocketUserMessage;
                var threadChannel = threadMessage.Channel as SocketThreadChannel;
                var forumChannel = threadChannel.ParentChannel;
                var threadGuild = threadChannel.Guild;
                var dbGuild = await Guild.GetGuild(threadGuild.Id.ToString());
                if(forumChannel.Id.ToString() == dbGuild.OpenAiChatForum)
                {
                    var embed = new EmbedBuilder()
                        .WithColor(Color.Blue)
                        .WithDescription("Hmm, let me think about that...")
                        .WithFooter("Powered by OpenAI GPT-4");
                    var responseEmbed = await threadChannel.SendMessageAsync(embeds: new []{embed.Build()});
                    var messages = await threadChannel.GetMessagesAsync(5).FlattenAsync();
                    messages = messages.OrderBy(m => m.CreatedAt);
                    //Setup OpenAI
                    var client = new OpenAIClient(new Uri(dbGuild.OpenAiURL), new AzureKeyCredential(dbGuild.OpenAiToken));
                    var model = "WahSpeech";
                    var complete = false;
                    int currentRetry = 0;
                    while (!complete && currentRetry < 2)
                    {
                        currentRetry++;
                        try
                        {
                            var chatCompletionsOptions = new ChatCompletionsOptions
                            {
                                MaxTokens = 1000,
                                Temperature = 0.5f,
                                PresencePenalty = 0.5f,
                                FrequencyPenalty = 0.5f
                            };
                            //Add messages to chat
                            chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.System,
                                "You are an AI assistant in a discord server, you must not use more than 4000 characters or 1000 tokens in a single response." +
                                "If you believe you need more characters/tokens to finish prompt the user to reply \"continue\" to start a new response"));
                            foreach (var message in messages)
                            {
                                //Get attached text file
                                var attachmentString = "";
                                if (message.Attachments.Count > 0)
                                {
                                    foreach (var attachment in message.Attachments)
                                    {
                                        //Download text as string using httpclient
                                        var httpClient = new HttpClient();
                                        attachmentString += $"\n{attachment.Filename}```{await httpClient.GetStringAsync(attachment.Url)}```\n";
                                    }
                                }
                                chatCompletionsOptions.Messages.Add(message.Author.IsBot
                                    ? new ChatMessage(ChatRole.Assistant, message.Embeds.FirstOrDefault()?.Description)
                                    : new ChatMessage(ChatRole.User, message.Content + attachmentString));
                            }
                            //Get response
                            var response = await client.GetChatCompletionsStreamingAsync(model, chatCompletionsOptions);
                            using var streamingChatCompletions = response.Value;
                            //setup embed and variable to update embed every 1 second
                            var nextSend = DateTime.Now.AddSeconds(1);
                            embed.WithDescription("");
                            embed.WithColor(Color.LightOrange);
                            //Stream response
                            await foreach (var choice in streamingChatCompletions.GetChoicesStreaming())
                            {
                                await foreach (var message in choice.GetMessageStreaming())
                                {
                                    embed.WithDescription(embed.Description + message.Content);
                                    embed.WithFooter($"Powered by OpenAI GPT-4, {model}, {embed.Description.Length}char");
                                    //If the timer has passed, update embed
                                    if (DateTime.Now <= nextSend) continue;
                                    await responseEmbed.ModifyAsync(m => m.Embeds = new[] { embed.Build() });
                                    nextSend = DateTime.Now.AddSeconds(1);
                                }
                            }
                            embed.WithColor(Color.Green);
                            complete = true;
                        }
                        catch (Exception e)
                        {
                            var response = e.Message;
                            complete = true;

                            if (e.Message.Contains("content management policy."))
                            {
                                response = "Failed to respond: Content is not allowed by Azure's content management policy.";
                            }
                            else if (e.Message.Contains("This model's maximum context length is"))
                            {
                                if (model == "WahSpeech32k")
                                {
                                    response = "Failed to respond: This conversation breaches the token limit, No more attempts";
                                }
                                else
                                {
                                    model = "WahSpeech32k";
                                    complete = false; // Allow retry with new model
                                }
                            }

                            if (complete)
                            {
                                embed.WithColor(Color.Red);
                                embed.WithFields(new EmbedFieldBuilder()
                                    .WithName("Error")
                                    .WithValue(response + "\n\n*Saying \"Continue\" should resume from where it stopped.*"));
                            }
                        }
                    }
                    await responseEmbed.ModifyAsync(m => m.Embeds = new []{embed.Build()});
                }
            }
        });
        return Task.CompletedTask;
    }
}