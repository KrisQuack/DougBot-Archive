using Discord;
using Discord.WebSocket;
using System.Text.RegularExpressions;

namespace DougBot.Systems;

public static class DeezNutz
{
    private static DiscordSocketClient _client;

    public static async Task Monitor()
    {
        _client = Program.Client;
        _client.MessageReceived += MessageReceivedHandler;
        Console.WriteLine("DMRelay Initialized");
    }

    private static Task MessageReceivedHandler(SocketMessage message)
    {
        _ = Task.Run(async () =>
        {
            //Cancel if the user is a mod
            //var user = message.Author as SocketGuildUser;
            //if (user.GuildPermissions.ModerateMembers || user.IsBot) { return; }
            //Check if the message matches this regex
            var regex = new Regex("(?i)(de[ei]s?[ez]|d[is]s|dietz) ?\\bnut[sz]?\\b.*|(?:\\bthese\\b|\\bthose\\b|\\bthem\\b|\\bthis\\b|\\bthe[msz]e\\b) ?\\bnut[sz]?\\b.*|\\bthe[msz]e\\b testicles");
            if (regex.IsMatch(message.Content))
            {
                if (message.Channel is SocketGuildChannel)
                {
                    //Get the guild
                    var guild = (message.Channel as SocketGuildChannel).Guild;
                    var logChannel = guild.GetTextChannel(886548334154760242);

                    //Get the embed
                    var embed = new EmbedBuilder()
                        .WithTitle("Deez Nutz")
                        .WithAuthor($"{message.Author.GlobalName}", message.Author.GetAvatarUrl())
                        .WithDescription(message.Content)
                        .Build();
                    //Send the embed
                    await logChannel.SendMessageAsync(embed: embed);
                }
            }
        });
        return Task.CompletedTask;
    }
}