using Discord;
using Discord.Rest;
using Discord.WebSocket;

namespace DougBot.Systems.EventBased
{
    public class ForumAutomod
    {
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
                    //Check if the parent channel is a forum
                    if (channel.ParentChannel.Id != 1133709621610156103) return;
                    //Get the current pinned messages
                    var pinnedMessages = await channel.GetPinnedMessagesAsync();
                    //If there are any pinned messages, return
                    if (pinnedMessages.Count > 0) return;
                    //Pin the first message
                    var messages = await channel.GetMessagesAsync(1).FlattenAsync();
                    var firstMessage = messages.FirstOrDefault();
                    if (firstMessage != null)
                    {
                        var pinMessage = firstMessage as SocketUserMessage;
                        if (pinMessage != null) await pinMessage.PinAsync();
                    }
                    //Post the welcome message
                    var embed = new EmbedBuilder()
                    {
                        Title = "Welcome to your new thread!",
                        Description = "Please remember the server rules still apply. " +
                        "If you have any issues, please contact the moderation team. More info:  https://discord.com/channels/567141138021089308/880127379119415306/1132052471481638932",
                        Color = Color.Orange,
                        Author = new EmbedAuthorBuilder()
                        {
                            Name = channel.Name,
                            IconUrl = channel.Guild.IconUrl
                        },
                    };
                    await channel.SendMessageAsync(embed: embed.Build());
                }
                catch (Exception e)
                {
                    Console.WriteLine($"[General/Warning] {DateTime.UtcNow:HH:mm:ss} ForumAutomod {e}");
                }
            });
            return Task.CompletedTask;
        }
    }
}
