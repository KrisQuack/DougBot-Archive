using System.Collections.Concurrent;
using Discord;
using Discord.WebSocket;

namespace DougBot.Systems.EventBased;

public class ForumAutomod
{
    private static readonly ConcurrentDictionary<ulong, DateTime> ProcessedThreads = new();

    public static async Task Monitor()
    {
        var client = Program.Client;
        client.ThreadCreated += ThreadCreated;
        Console.WriteLine("Forum Automod Initialized");
    }

    private static Task ThreadCreated(SocketThreadChannel channel)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                // Check if the thread has already been processed
                if (!ProcessedThreads.TryAdd(channel.Id, DateTime.UtcNow)) // 0 is a dummy value
                    return;
                await Task.Delay(1000);
                //Check if the parent channel is a forum
                if (channel.ParentChannel.Id != 1133709621610156103) return;
                //Pin the first message
                var messages = await channel.GetMessagesAsync(10).FlattenAsync();
                var firstMessage = messages.OrderBy(m => m.CreatedAt).FirstOrDefault();
                if (firstMessage != null)
                {
                    var pinMessage = firstMessage as SocketUserMessage;
                    if (pinMessage != null) await pinMessage.PinAsync();
                }

                //Post the welcome message
                var embed = new EmbedBuilder
                {
                    Title = "Welcome to your new thread!",
                    Description = "Please remember the server rules still apply. " +
                                  "If you have any issues, please contact the moderation team. More info:  https://discord.com/channels/567141138021089308/880127379119415306/1132052471481638932\n" +
                                  $"<@{firstMessage.Author.Id}> as the owner you may also Pin and Unpin posts in your thread, Just right click (hold down on mobile) on a message, select Apps and then Pin or Unpin." +
                                  " [Example](https://cdn.discordapp.com/attachments/886548334154760242/1135511848817545236/image.png)",
                    Color = Color.Orange,
                    Author = new EmbedAuthorBuilder
                    {
                        Name = channel.Name,
                        IconUrl = channel.Guild.IconUrl
                    }
                };
                await channel.SendMessageAsync(embed: embed.Build());
                //remove the thread from the dictionary older than 10 minutes
                var oldKeys = ProcessedThreads.Where(kvp => kvp.Value < DateTime.UtcNow.AddMinutes(-10))
                    .Select(kvp => kvp.Key);
                foreach (var key in oldKeys) ProcessedThreads.TryRemove(key, out _);
            }
            catch (Exception e)
            {
                Console.WriteLine($"[General/Warning] {DateTime.UtcNow:HH:mm:ss} ForumAutomod {e}");
            }
        });
        return Task.CompletedTask;
    }
}