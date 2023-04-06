using System.Reflection.Emit;
using System.Text.Json;
using Discord;
using DougBot.Models;
using TwitchLib.Api;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Enums;
using TwitchLib.PubSub.Events;
using TwitchLib.PubSub.Models.Responses.Messages.AutomodCaughtMessage;

namespace DougBot.Twitch;

public class PubSub
{
    private TwitchPubSub Client;

    public TwitchPubSub Initialize()
    {
        Client = new TwitchPubSub();
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
                    var endDateOffset = new DateTimeOffset(endDate).ToUnixTimeSeconds();
                    messageContent = "<@&1080237787174948936>";
                    embed.WithTitle($"Prediction Created: {Prediction.Title}");
                    embed.WithDescription($"Voting ends: <t:{endDateOffset}:R>");
                    embed.WithColor(Color.Green);
                    embed.AddField("Outcomes", string.Join("\n", Prediction.Outcomes.Select(p => $"{p.Title}")));
                }
                //Canceled
                else if (Prediction.Type == PredictionType.EventUpdated && Prediction.Status == PredictionStatus.Canceled)
                {
                    embed.WithTitle($"Prediction Canceled: {Prediction.Title}");
                    embed.WithColor(Color.Red);
                }
                //Locked or Resolved
                else if (Prediction.Type == PredictionType.EventUpdated && (Prediction.Status == PredictionStatus.Locked || Prediction.Status == PredictionStatus.Resolved))
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
                            (!isResolved ?
                                //Locked
                                string.Join("\n", outcome.TopPredictors
                                    .OrderByDescending(p => p.Points)
                                    .Take(5)
                                    .Select(p => $"{p.DisplayName} bet {p.Points:n0}"))+"\n" : 
                                isWinning ?
                                    //Resolved : Win
                                    string.Join("\n", outcome.TopPredictors
                                        .OrderByDescending(p => p.Points)
                                        .Take(5)
                                        .Select(p => $"{p.DisplayName} won {p.Points * ((double)totalPoints / winOutcome.TotalPoints):n0} points"))+"\n" :
                                    //Resolved : Loss
                                    string.Join("\n", outcome.TopPredictors
                                        .OrderByDescending(p => p.Points)
                                        .Take(5)
                                        .Select(p => $"{p.DisplayName} lost {p.Points:n0}"))+"\n"
                                    ), true);
                    }
                }
                //Check the embed is not empty
                if (embed.Title == null)
                {
                    Console.WriteLine($"Prediction event not handled: {Prediction.Type} - {Prediction.Status}");
                    return;
                }
                //Send message
                var embedJson = JsonSerializer.Serialize(new List<EmbedBuilder> { embed },
                    new JsonSerializerOptions { Converters = { new ColorJsonConverter() } });
                var dict = new Dictionary<string, string>
                {
                    { "guildId", "567141138021089308" },
                    { "channelId", "1070317311505997864" },
                    { "message", messageContent },
                    { "embedBuilders", embedJson },
                    { "ping", "true" },
                    { "attachments", null }
                };
                new Queue("SendMessage", 1, dict, null).Insert();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        });
    }

    private void PubSub_OnChannelPointsRewardRedeemed(object sender, OnChannelPointsRewardRedeemedArgs Redemption)
    {
        _ = Task.Run(async () =>
        {
            var redemption = Redemption.RewardRedeemed.Redemption;
            var reward = redemption.Reward;
            var redeemedUser = redemption.User;
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
                var dict = new Dictionary<string, string>
                {
                    { "guildId", "567141138021089308" },
                    { "channelId", "1080251555619557445" },
                    { "message", "" },
                    { "embedBuilders", embedJson },
                    { "ping", "true" },
                    { "attachments", null }
                };
                await new Queue("SendMessage", 1, dict, null).Insert();
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