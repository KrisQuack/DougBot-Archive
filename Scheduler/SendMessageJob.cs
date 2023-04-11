using System.Text.Json;
using Discord;
using Discord.WebSocket;
using DougBot.Models;
using Quartz;
using JsonSerializerOptions = System.Text.Json.JsonSerializerOptions;

namespace DougBot.Scheduler;

public class SendMessageJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        var dataMap = context.JobDetail.JobDataMap;
        var guildId = Convert.ToUInt64(dataMap.GetString("guildId"));
        var channelId = Convert.ToUInt64(dataMap.GetString("channelId"));
        var message = dataMap.GetString("message");
        var embedBuilders = dataMap.GetString("embedBuilders");
        var componentBuilder = dataMap.GetString("componentBuilder");
        var ping = dataMap.GetBoolean("ping");
        var attachments = dataMap.GetString("attachments");

        //check for nulls and return if any are null
        if (guildId == 0 || channelId == 0 || message == null)
            return;
        
        // Get the guild and channel
        var guild = Program._Client.GetGuild(guildId);
        var channel = guild.Channels.FirstOrDefault(x => x.Id == channelId) as SocketTextChannel;

        // Deserialize and process embeds (if any)
        List<Embed> embeds = null;
        if (!string.IsNullOrEmpty(embedBuilders))
        {
            var embedBuildersList = JsonSerializer.Deserialize<List<EmbedBuilder>>(embedBuilders,
                new JsonSerializerOptions { Converters = { new ColorJsonConverter() } });

            foreach (var embed in embedBuildersList.Where(embed => embed.Url != null && embed.ImageUrl == null))
                embed.WithImageUrl(embed.Url);

            embeds = embedBuildersList.Select(embed => embed.Build()).ToList();
        }

        // Deserialize and process components (if any)
        ComponentBuilder componentBuilderObj = null;
        if (!string.IsNullOrEmpty(componentBuilder))
        {
            componentBuilderObj = JsonSerializer.Deserialize<ComponentBuilder>(componentBuilder);
        }

        // Send the message with embeds and components (if any)
        await channel.SendMessageAsync(message,
            embeds: embeds?.ToArray(),
            components: componentBuilderObj?.Build(),
            allowedMentions: ping ? AllowedMentions.All : AllowedMentions.None);

        // Send attachments (if any)
        if (!string.IsNullOrEmpty(attachments))
        {
            var attachList = JsonSerializer.Deserialize<List<string>>(attachments);
            foreach (var attachment in attachList)
            {
                await channel.SendFileAsync(attachment, attachment.Split('/').LastOrDefault(x => x.Contains('.')));
                // Delete file
                File.Delete(attachment);
            }
        }
    }
}