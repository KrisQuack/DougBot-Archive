using System.Text.Json;
using Discord;
using DougBot.Models;
using DougBot.Scheduler;
using Quartz;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Enums;
using TwitchLib.PubSub.Events;
using JsonSerializerOptions = System.Text.Json.JsonSerializerOptions;

namespace DougBot.Twitch;

public class PubSub
{
    public TwitchPubSub Create()
    {
        var Client = new TwitchPubSub();
        //Main events
        Client.OnListenResponse += OnListenResponse;
        Client.OnPubSubServiceClosed += OnPubSubServiceClosed;
        Client.OnPubSubServiceError += OnPubSubServiceError;
        //Listeners
        Client.OnPrediction += PubSub_OnPrediction;
        Client.OnChannelPointsRewardRedeemed += PubSub_OnChannelPointsRewardRedeemed;
        return Client;
    }

    private void PubSub_OnPrediction(object? sender, OnPredictionArgs Prediction)
    {
        _ = Task.Run(async () =>
        {
            try
            {
                //create base embed
                var embed = new EmbedBuilder()
                    .WithCurrentTimestamp();
                var messageContent = "";
                //New
                if (Prediction.Type == PredictionType.EventCreated)
                {
                    var endDate = Prediction.CreatedAt.Value.AddSeconds(Prediction.PredictionTime);
                    var endDateOffset = new DateTimeOffset(endDate).ToUniversalTime().ToUnixTimeSeconds();
                    messageContent = "<@&1080237787174948936>";
                    embed.WithTitle($"Prediction Created: {Prediction.Title}");
                    embed.WithDescription($"Voting ends: <t:{endDateOffset}:R>");
                    embed.WithColor(Color.Green);
                    embed.AddField("Outcomes", string.Join("\n", Prediction.Outcomes.Select(p => $"{p.Title}")));
                }
                //Canceled
                else if (Prediction.Type == PredictionType.EventUpdated &&
                         Prediction.Status == PredictionStatus.Canceled)
                {
                    embed.WithTitle($"Prediction Canceled: {Prediction.Title}");
                    embed.WithColor(Color.Red);
                }
                //Locked or Resolved
                else if (Prediction.Type == PredictionType.EventUpdated &&
                         (Prediction.Status == PredictionStatus.Locked ||
                          Prediction.Status == PredictionStatus.Resolved))
                {
                    var isResolved = Prediction.Status == PredictionStatus.Resolved;
                    embed.WithTitle($"Prediction {(isResolved ? "Ended" : "Locked")}: {Prediction.Title}");
                    embed.WithColor(isResolved ? Color.Orange : Color.Blue);
                    var winOutcome = Prediction.Outcomes.FirstOrDefault(p => p.Id == Prediction.WinningOutcomeId);
                    var totalPoints = Prediction.Outcomes.Sum(p => p.TotalPoints);
                    var totalUsers = Prediction.Outcomes.Sum(p => p.TotalUsers);
                    //Create field for each loosing outcome
                    foreach (var outcome in Prediction.Outcomes)
                    {
                        var isWinning = winOutcome != null && outcome.Id == Prediction.WinningOutcomeId;
                        embed.AddField(isWinning ? $"🎉{outcome.Title}🎉" : $"{outcome.Title}",
                            $"Users: **{outcome.TotalUsers:n0}** {Math.Round((double)outcome.TotalUsers / totalUsers * 100, 0)}%\n" +
                            $"Points: **{outcome.TotalPoints:n0}** {Math.Round((double)outcome.TotalPoints / totalPoints * 100, 0)}%\n" +
                            $"Ratio: 1:{Math.Round((double)totalPoints / outcome.TotalPoints, 2)}\n\n" +
                            $"**__High Rollers__**\n" +
                            (!isResolved
                                ?
                                //Locked
                                string.Join("\n", outcome.TopPredictors
                                    .OrderByDescending(p => p.Points)
                                    .Take(5)
                                    .Select(p => $"{p.DisplayName} bet {p.Points:n0}")) + "\n"
                                : isWinning
                                    ?
                                    //Resolved : Win
                                    string.Join("\n", outcome.TopPredictors
                                        .OrderByDescending(p => p.Points)
                                        .Take(5)
                                        .Select(p =>
                                            $"{p.DisplayName} won {p.Points * ((double)totalPoints / winOutcome.TotalPoints):n0} points")) +
                                    "\n"
                                    :
                                    //Resolved : Loss
                                    string.Join("\n", outcome.TopPredictors
                                        .OrderByDescending(p => p.Points)
                                        .Take(5)
                                        .Select(p => $"{p.DisplayName} lost {p.Points:n0}")) + "\n"
                            ), true);
                    }
                }

                //Quack Cheat
                await QuackCheat(Prediction);

                //Check the embed is not empty
                if (string.IsNullOrEmpty(embed.Title)) return;
                //Send message
                var embedJson = JsonSerializer.Serialize(new List<EmbedBuilder> { embed },
                    new JsonSerializerOptions { Converters = { new ColorJsonConverter() } });
                var sendMessageJob = JobBuilder.Create<SendMessageJob>()
                    .WithIdentity($"sendMessageJob-{Guid.NewGuid()}", "567141138021089308")
                    .UsingJobData("guildId", "567141138021089308")
                    .UsingJobData("channelId", "1070317311505997864")
                    .UsingJobData("message", messageContent)
                    .UsingJobData("embedBuilders", embedJson)
                    .UsingJobData("ping", "true")
                    .UsingJobData("attachments", null)
                    .Build();
                var sendMessageTrigger = TriggerBuilder.Create()
                    .WithIdentity($"sendMessageTrigger-{Guid.NewGuid()}", "567141138021089308")
                    .StartNow()
                    .Build();
                await Scheduler.Quartz.MemorySchedulerInstance.ScheduleJob(sendMessageJob, sendMessageTrigger);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        });

        async Task QuackCheat(OnPredictionArgs Prediction)
        {
            var endTime = Prediction.CreatedAt.Value.ToUniversalTime().AddSeconds(Prediction.PredictionTime);
            var timeRemaining = endTime - DateTime.UtcNow;
            if (Prediction.Status == PredictionStatus.Active &&
                new[] { 10, 20, 30, 60 }.Contains((int)timeRemaining.TotalSeconds))
            {
                var cheatEmbed = new EmbedBuilder()
                    .WithCurrentTimestamp()
                    .WithTitle($"{(int)timeRemaining.TotalSeconds}: {Prediction.Title}")
                    .WithColor(Color.DarkGrey);
                foreach (var outCome in Prediction.Outcomes)
                {
                    var cheatHighRollers = outCome.TopPredictors.OrderByDescending(p => p.Points).ToList();
                    cheatEmbed.AddField($"{outCome.Title} - {cheatHighRollers.Sum(c => c.Points):n0}",
                        string.Join("\n", cheatHighRollers.Select(p => $"{p.DisplayName} - {p.Points:n0}")), true);
                }

                var discClient = Program._Client;
                var guild = discClient.GetGuild(567141138021089308);
                var channel = guild.GetTextChannel(886548334154760242);
                var message = await channel.SendMessageAsync("", embed: cheatEmbed.Build());
                //Schedule message removal
                var deleteMessageJob = JobBuilder.Create<DeleteMessageJob>()
                    .WithIdentity($"deleteMessageJob-{Guid.NewGuid()}", "567141138021089308")
                    .UsingJobData("guildId", "567141138021089308")
                    .UsingJobData("channelId", message.Channel.Id)
                    .UsingJobData("messageId", message.Id)
                    .Build();
                var deleteMessageTrigger = TriggerBuilder.Create()
                    .WithIdentity($"sendMessageTrigger-{Guid.NewGuid()}", "567141138021089308")
                    .StartAt(endTime)
                    .Build();
                await Scheduler.Quartz.MemorySchedulerInstance.ScheduleJob(deleteMessageJob, deleteMessageTrigger);
            }
        }
    }

    private void PubSub_OnChannelPointsRewardRedeemed(object sender, OnChannelPointsRewardRedeemedArgs Redemption)
    {
        _ = Task.Run(async () =>
        {
            var redemption = Redemption.RewardRedeemed.Redemption;
            var reward = redemption.Reward;
            var redeemedUser = redemption.User;
            //Notify mods of new redemption
            if (redemption.Status == "UNFULFILLED")
            {
                var embed = new EmbedBuilder()
                    .WithTitle($"New Redemption: {reward.Title}")
                    .WithColor(Color.Orange)
                    .WithFields(
                        new EmbedFieldBuilder()
                            .WithName("Redeemed By")
                            .WithValue(redeemedUser.DisplayName)
                            .WithIsInline(true),
                        new EmbedFieldBuilder()
                            .WithName("Message")
                            .WithValue(redemption.UserInput ?? "No Message")
                            .WithIsInline(true))
                    .WithCurrentTimestamp();
                var embedJson = JsonSerializer.Serialize(new List<EmbedBuilder> { embed },
                    new JsonSerializerOptions { Converters = { new ColorJsonConverter() } });
                var sendMessageJob = JobBuilder.Create<SendMessageJob>()
                    .WithIdentity($"sendMessageJob-{Guid.NewGuid()}", "567141138021089308")
                    .UsingJobData("guildId", "567141138021089308")
                    .UsingJobData("channelId", "1080251555619557445")
                    .UsingJobData("message", "")
                    .UsingJobData("embedBuilders", embedJson)
                    .UsingJobData("ping", "true")
                    .UsingJobData("attachments", null)
                    .Build();
                var sendMessageTrigger = TriggerBuilder.Create()
                    .WithIdentity($"sendMessageTrigger-{Guid.NewGuid()}", "567141138021089308")
                    .StartNow()
                    .Build();
                await Scheduler.Quartz.MemorySchedulerInstance.ScheduleJob(sendMessageJob, sendMessageTrigger);
            }
        });
    }

    private void OnPubSubServiceError(object sender, OnPubSubServiceErrorArgs e)
    {
        Console.WriteLine($"{e.Exception.Message}");
    }

    private void OnPubSubServiceClosed(object sender, EventArgs e)
    {
        Console.WriteLine("Connection closed to pubsub server");
    }

    private void OnListenResponse(object sender, OnListenResponseArgs e)
    {
        if (!e.Successful) Console.WriteLine($"Failed to listen! {e.Topic} {e.Response.Error}");
    }
}