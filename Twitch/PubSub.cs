using System.Reflection.Emit;
using System.Text.Json;
using Discord;
using DougBot.Models;
using TwitchLib.Api;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Enums;
using TwitchLib.PubSub.Events;

namespace DougBot.Twitch;

public class PubSub
{
    private TwitchAPI API;
    private TwitchPubSub Client;
    private string DougToken;

    public TwitchPubSub Initialize(TwitchAPI api, string dougToken, string channelID)
    {
        API = api;
        DougToken = dougToken;
        Client = new TwitchPubSub();
        //Main events
        Client.OnListenResponse += OnListenResponse;
        Client.OnPubSubServiceConnected += OnPubSubServiceConnected;
        Client.OnPubSubServiceClosed += OnPubSubServiceClosed;
        Client.OnPubSubServiceError += OnPubSubServiceError;
        //Listeners
        Client.OnPrediction += PubSub_OnPrediction;
        Client.ListenToPredictions(channelID);
        Client.OnChannelPointsRewardRedeemed += PubSub_OnChannelPointsRewardRedeemed;
        Client.ListenToChannelPoints(channelID);

        Client.Connect();

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
                else if (Prediction.Type == PredictionType.EventUpdated &&
                         Prediction.Status == PredictionStatus.Canceled)
                {
                    embed.WithTitle($"Prediction Canceled: {Prediction.Title}");
                    embed.WithColor(Color.Red);
                }
                else if (Prediction.Type == PredictionType.EventUpdated &&
                         (Prediction.Status == PredictionStatus.Resolved || Prediction.Status == PredictionStatus.Locked))
                {
                    var status = Prediction.Status == PredictionStatus.Resolved ? "Ended" : "Locked";
                    embed.WithTitle($"Prediction {status}: {Prediction.Title}");
                    embed.WithColor(Prediction.Status == PredictionStatus.Resolved ? Color.Orange : Color.Gold);
                    //Get result
                    var winOutcome = Prediction.Outcomes.FirstOrDefault(p => p.Id == Prediction.WinningOutcomeId);
                    //Create field for winning outcome
                    var totalPoints = Prediction.Outcomes.Sum(p => p.TotalPoints);
                    var totalUsers = Prediction.Outcomes.Sum(p => p.TotalUsers);
                    //Create field for each loosing outcome
                    foreach (var outcome in Prediction.Outcomes)
                    {
                        var isWinner = outcome.Id == Prediction.WinningOutcomeId;
                        embed.AddField(isWinner ? $"🎉 {outcome.Title} 🎉" : $"{outcome.Title}",
                            $"Users: **{outcome.TotalUsers:n0}** {Math.Round((double)outcome.TotalUsers / totalUsers * 100, 0)}%\n" +
                            $"Points: **{outcome.TotalPoints:n0}** {Math.Round((double)outcome.TotalPoints / totalPoints * 100, 0)}%\n" +
                            $"Ratio: 1:{Math.Round((double)totalPoints / outcome.TotalPoints, 2)}");
                    }
                    embed.AddField("__Biggest Losers__",
                        string.Join("\n", Prediction.Outcomes.Where(p => p.Id != winOutcome.Id)
                            .SelectMany(p => p.TopPredictors)
                            .OrderByDescending(p => p.Points).Take(5)
                            .Select(p => $"{p.DisplayName} {(Prediction.Status == PredictionStatus.Resolved ? "lost" : "bet")} {p.Points:n0}")));
                    embed.AddField("__Biggest Winners__",
                        string.Join("\n", winOutcome.TopPredictors.OrderByDescending(p => p.Points).Take(5)
                            .Select(p =>
                                $"{p.DisplayName} bet {p.Points:n0} won {p.Points * ((double)totalPoints / winOutcome.TotalPoints):n0} points")));
                }

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

    private void OnPubSubServiceConnected(object sender, EventArgs e)
    {
        Console.WriteLine("Connected to pubsub server");
        Client.SendTopics(DougToken);
    }

    private void OnListenResponse(object sender, OnListenResponseArgs e)
    {
        if (!e.Successful) Console.WriteLine($"Failed to listen! {e.Topic} {e.Response.Error}");
    }
}