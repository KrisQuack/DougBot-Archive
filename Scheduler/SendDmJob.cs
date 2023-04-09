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
            var dataMap = context.JobDetail.JobDataMap;
            var client = Program._Client;
            var guildId = Convert.ToUInt64(dataMap.GetString("guildId"));
            var userId = Convert.ToUInt64(dataMap.GetString("userId"));
            var senderId = Convert.ToUInt64(dataMap.GetString("senderId"));
            var embedBuilders = dataMap.GetString("embedBuilders");

            var dbGuild = await Guild.GetGuild(guildId.ToString());
            var guild = client.Guilds.FirstOrDefault(g => g.Id == guildId);
            var channel = guild.Channels.FirstOrDefault(c => c.Id.ToString() == dbGuild.DmReceiptChannel) as SocketTextChannel;
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
                    .WithAuthor($"DM to {user.Username}#{user.Discriminator} ({user.Id}) from {sender.Username}",
                        sender.GetAvatarUrl())
                    .Build()).ToList();
            await channel.SendMessageAsync(embeds: embeds.ToArray());
        }
    }