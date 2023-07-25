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
        try
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
            var guild = Program.Client.GetGuild(guildId);
            var channel = guild.GetTextChannel(Convert.ToUInt64(channelId));

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
                componentBuilderObj = JsonSerializer.Deserialize<ComponentBuilder>(componentBuilder);
            
            // Send attachments (if any) in their own embed
            if (!string.IsNullOrEmpty(attachments) && attachments != "null" && attachments != "[]")
            {
                var attachmentsList = JsonSerializer.Deserialize<List<string>>(attachments);
                foreach (var attachment in attachmentsList)
                {
                    var attachmentEmbed = new EmbedBuilder()
                        .WithTitle(attachment.Split('/').Last().Split('\\').Last())
                        .WithUrl(attachment)
                        .WithImageUrl(attachment)
                        .Build();
                    embeds.Add(attachmentEmbed);
                }
            }
            await channel.SendMessageAsync(message,
                    embeds: embeds?.ToArray(),
                    components: componentBuilderObj?.Build(),
                    allowedMentions: ping ? AllowedMentions.All : AllowedMentions.None);
        }
        catch (Exception e)
        {
            Console.WriteLine($"[General/Warning] {DateTime.UtcNow:HH:mm:ss} SendMessageJob {e}");
        }
    }

    public static async Task Queue(string guildId, string channelId, List<EmbedBuilder> embeds, DateTime schedule,
        string message = "", bool ping = false, List<string>? attachments = null)
    {
        try
        {
            var embedJson = JsonSerializer.Serialize(embeds,
                new JsonSerializerOptions { Converters = { new ColorJsonConverter() } });
            var attachmentsJson = JsonSerializer.Serialize(attachments);
            var sendMessageJob = JobBuilder.Create<SendMessageJob>()
                .WithIdentity($"sendMessageJob-{Guid.NewGuid()}", guildId)
                .UsingJobData("guildId", guildId)
                .UsingJobData("channelId", channelId)
                .UsingJobData("message", message)
                .UsingJobData("embedBuilders", embedJson)
                .UsingJobData("ping", ping)
                .UsingJobData("attachments", attachmentsJson)
                .Build();
            var sendMessageTrigger = TriggerBuilder.Create()
                .WithIdentity($"sendMessageTrigger-{Guid.NewGuid()}", guildId)
                .StartAt(schedule)
                .Build();
            if (schedule > DateTime.UtcNow.AddMinutes(10))
                await Quartz.PersistentSchedulerInstance.ScheduleJob(sendMessageJob, sendMessageTrigger);
            else
                await Quartz.MemorySchedulerInstance.ScheduleJob(sendMessageJob, sendMessageTrigger);
        }
        catch (Exception e)
        {
            Console.WriteLine($"[General/Warning] {DateTime.UtcNow:HH:mm:ss} SendMessageQueue {e}");
        }
    }
}