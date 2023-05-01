using Azure;
using Azure.AI.OpenAI;
using Common;
using Discord;
using Discord.WebSocket;
using DougBot.Models;
using DougBot.Systems;
using DougBot;
using System.Text;
using System.Linq;

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
            if (!arg.Author.IsBot && arg.Channel.GetType() == typeof(SocketThreadChannel))
            {
                var threadMessage = arg as SocketUserMessage;
                var threadChannel = threadMessage.Channel as SocketThreadChannel;
                var forumChannel = threadChannel.ParentChannel;
                var threadGuild = threadChannel.Guild;
                var dbGuild = await Guild.GetGuild(threadGuild.Id.ToString());
                if (forumChannel.Id.ToString() == dbGuild.OpenAiChatForum)
                {
                    var initialResponseMessage = await threadChannel.SendMessageAsync("Processing request, this may take some time depending on the request...");
                    var messages = await threadChannel.GetMessagesAsync(20).FlattenAsync();
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
                                MaxTokens = 2000,
                                Temperature = 0.5f,
                                PresencePenalty = 0.5f,
                                FrequencyPenalty = 0.5f
                            };
                            //Add messages to chat
                            chatCompletionsOptions.Messages.Add(new ChatMessage(ChatRole.System,"You are an AI assistant in a discord server, you have no maximum character limit."));
                            using var httpClient = new HttpClient();
                            foreach (var message in messages)
                            {
                                //Get attached text file
                                var attachmentString = "";
                                if (message.Attachments.Count > 0)
                                {
                                    foreach (var attachment in message.Attachments)
                                    {
                                        //Download text as string using httpclient
                                        attachmentString += $"\n{attachment.Filename}```{await httpClient.GetStringAsync(attachment.Url)}```\n";
                                    }
                                }
                                chatCompletionsOptions.Messages.Add(message.Author.IsBot
                                    ? new ChatMessage(ChatRole.Assistant, string.Join("\n",message.Embeds.Select(e => e.Description)))
                                    : new ChatMessage(ChatRole.User, message.Content + attachmentString));
                            }
                            //Get response
                            var completionResponse = await client.GetChatCompletionsAsync(model, chatCompletionsOptions);
                            var chatCompletions = completionResponse.Value;
                            await threadChannel.SendMessageAsync(embeds: SplitMessage(chatCompletions.Choices[0].Message.Content).ToArray());
                            await initialResponseMessage.DeleteAsync();
                            //Create fields for audit log
                            var fields = new List<EmbedFieldBuilder>
                            {
                                new EmbedFieldBuilder
                                {
                                    Name = "User",
                                    Value = threadMessage.Author.Mention,
                                    IsInline = true
                                },
                                new EmbedFieldBuilder
                                {
                                    Name = "Channel",
                                    Value = threadChannel.Mention,
                                    IsInline = true
                                },
                                new EmbedFieldBuilder
                                {
                                    Name = "Tokens",
                                    Value = $"Prompt: {chatCompletions.Usage.PromptTokens}\n" +
                                    $"Completion: {chatCompletions.Usage.CompletionTokens}\n" +
                                    $"Total: {chatCompletions.Usage.TotalTokens}",
                                    IsInline = false
                                }
                            };
                            //Author
                            var author = new EmbedAuthorBuilder
                            {
                                Name = threadMessage.Author.Username,
                                IconUrl = threadMessage.Author.GetAvatarUrl()
                            };
                            //Send audit log
                            await AuditLog.LogEvent("OpenAI", threadGuild.Id.ToString(), Color.Green, fields, author);
                            complete = true;
                        }
                        catch (Exception e)
                        {
                            Console.WriteLine(e.ToString());
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
                                await initialResponseMessage.ModifyAsync(m => m.Content = response + "\n\n*Saying \"Continue\" should resume from where it stopped.*");
                            }
                        }
                    }
                }
            }
        });
        return Task.CompletedTask;
    }

    private static List<Embed> SplitMessage(string message)
    {
        var splitMessage = message.Split("```");
        var messages = new List<string>();
        var isCode = false;

        // Helper function to add a message part with the correct delimiters
        void AddMessagePart(StringBuilder currentPart, bool isCodePart)
        {
            if (currentPart.Length > 0)
            {
                messages.Add(isCodePart ? $"```{currentPart}```" : currentPart.ToString());
                currentPart.Clear();
            }
        }

        foreach (var part in splitMessage)
        {
            var splitPart = part.Split('\n');
            var currentPart = new StringBuilder();

            foreach (var line in splitPart)
            {
                // If the current part length + line length > 4000, add the current part to the messages
                if (currentPart.Length + line.Length > 4000)
                {
                    AddMessagePart(currentPart, isCode);
                }
                currentPart.Append(line).Append('\n');
            }

            // Add the last part if not empty
            AddMessagePart(currentPart, isCode);

            isCode = !isCode;
        }

        // Combine messages that are less than 4000 characters together if the total is still less than 2000 characters
        var combinedMessages = new List<string>();
        var currentMessage = new StringBuilder();

        foreach (var part in messages)
        {
            if (currentMessage.Length + part.Length > 4000)
            {
                combinedMessages.Add(currentMessage.ToString());
                currentMessage.Clear();
            }
            currentMessage.Append(part);
        }
        // Add the last message if not empty
        if (currentMessage.Length > 0)
        {
            combinedMessages.Add(currentMessage.ToString());
        }
        //Add all the messages to a list of embeds
        var embeds = new List<Embed>();
        foreach (var msg in combinedMessages)
        {
            embeds.Add(new EmbedBuilder
            {
                Description = msg
            }.Build());
        }

        return embeds;
    }
}