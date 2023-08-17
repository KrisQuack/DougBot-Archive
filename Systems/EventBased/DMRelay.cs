using Discord;
using Discord.WebSocket;
using Fernandezja.ColorHashSharp;

namespace DougBot.Systems.EventBased;

public static class DMRelay
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
            if (message.Channel is SocketDMChannel && message.Author.MutualGuilds.Any() &&
                message.Author.Id != _client.CurrentUser.Id)
            {
                //Create embed to send to guild
                var embeds = new List<EmbedBuilder>();
                //Main embed
                var colorHash = new ColorHash();
                var color = colorHash.BuildToColor(message.Author.Id.ToString());
                embeds.Add(new EmbedBuilder()
                    .WithDescription(message.Content)
                    .WithColor((Color)color)
                    .WithAuthor(new EmbedAuthorBuilder()
                        .WithName($"{message.Author.Username} ({message.Author.Id})")
                        .WithIconUrl(message.Author.GetAvatarUrl()))
                    .WithCurrentTimestamp());
                //Attachment embeds
                embeds.AddRange(message.Attachments.Select(attachment =>
                    new EmbedBuilder().WithTitle(attachment.Filename).WithImageUrl(attachment.Url)
                        .WithColor((Color)color)
                        .WithAuthor(new EmbedAuthorBuilder()
                            .WithName($"{message.Author.Username} ({message.Author.Id})")
                            .WithIconUrl(message.Author.GetAvatarUrl()))
                        .WithUrl(attachment.Url)
                        .WithCurrentTimestamp()));
                //Confirm message and where to send
                await ConfigurationService.Instance.DmReceiptChannel.SendMessageAsync(
                    embeds: embeds.Select(embed => embed.Build()).ToArray());
            }
        });
        return Task.CompletedTask;
    }
}