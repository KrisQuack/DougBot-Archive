using System.Text.Json;
using Discord;
using Discord.WebSocket;
using DougBot.Models;
using Fernandezja.ColorHashSharp;
using Quartz;
using JsonSerializerOptions = System.Text.Json.JsonSerializerOptions;

namespace DougBot.Scheduler;

public class SendDmJob : IJob
{
    public async Task Execute(IJobExecutionContext context)
    {
        try
        {
            var dataMap = context.JobDetail.JobDataMap;
            var client = Program.Client;
            var guildId = Convert.ToUInt64(dataMap.GetString("guildId"));
            var userId = Convert.ToUInt64(dataMap.GetString("userId"));
            var senderId = Convert.ToUInt64(dataMap.GetString("senderId"));
            var embedBuilders = dataMap.GetString("embedBuilders");

            //check for nulls and return if any are null
            if (guildId == 0 || userId == 0 || senderId == 0 || embedBuilders == null)
                return;

            var dbGuild = await Guild.GetGuild(guildId.ToString());
            var guild = client.Guilds.FirstOrDefault(g => g.Id == guildId);
            var channel =
                guild.Channels.FirstOrDefault(c => c.Id.ToString() == dbGuild.DmReceiptChannel) as SocketTextChannel;
            var user = await client.GetUserAsync(userId);
            var sender = await client.GetUserAsync(senderId);

            var embedBuildersList = JsonSerializer.Deserialize<List<EmbedBuilder>>(embedBuilders,
                new JsonSerializerOptions { Converters = { new ColorJsonConverter() } });

            var embeds = embedBuildersList.Select(embed => embed.Build()).ToList();
            string status;
            var color = (Color)embeds[0].Color;
            var colorHash = new ColorHash();

            try
            {
                await user.SendMessageAsync(embeds: embeds.ToArray());
                status = "Message Delivered";
                color = (Color)colorHash.BuildToColor(userId.ToString());
            }
            catch (Exception ex)
            {
                if (ex.Message.Contains("Cannot send messages to this user"))
                    status = "User has blocked DMs";
                else
                    status = "Error: " + ex.Message;
                color = Color.Red;
            }

            embeds = embedBuildersList.Select(embed =>
                embed.WithTitle(status)
                    .WithColor(color)
                    .WithAuthor($"DM to {user.Username} ({user.Id}) from {sender.Username}",
                        sender.GetAvatarUrl())
                    .Build()).ToList();
            await channel.SendMessageAsync(embeds: embeds.ToArray());
        }
        catch (Exception e)
        {
            Console.WriteLine($"[General/Warning] {DateTime.UtcNow:HH:mm:ss} SendDmJob {e}");
        }
    }

    public static async Task Queue(string recieverId, string senderId, string guildId, List<EmbedBuilder> embeds,
        DateTime schedule)
    {
        try
        {
            var embedJson = JsonSerializer.Serialize(embeds,
                new JsonSerializerOptions { Converters = { new ColorJsonConverter() } });
            var sendDmJob = JobBuilder.Create<SendDmJob>()
                .WithIdentity($"sendDMJob-{Guid.NewGuid()}", guildId)
                .UsingJobData("guildId", guildId)
                .UsingJobData("userId", recieverId)
                .UsingJobData("embedBuilders", embedJson)
                .UsingJobData("senderId", senderId)
                .Build();
            var sendDmTrigger = TriggerBuilder.Create()
                .WithIdentity($"sendDMTrigger-{Guid.NewGuid()}", guildId)
                .StartAt(schedule)
                .Build();
            if (schedule > DateTime.UtcNow.AddMinutes(10))
                await Quartz.PersistentSchedulerInstance.ScheduleJob(sendDmJob, sendDmTrigger);
            else
                await Quartz.MemorySchedulerInstance.ScheduleJob(sendDmJob, sendDmTrigger);
        }
        catch (Exception e)
        {
            Console.WriteLine($"[General/Warning] {DateTime.UtcNow:HH:mm:ss} SendDmQueue {e}");
        }
    }
}