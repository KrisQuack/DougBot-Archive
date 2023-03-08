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
        try
        {
            _ = Task.Run(async () =>
            {
                //create base embed
                var embed = new EmbedBuilder()
                    .WithCurrentTimestamp();
                long totalPoints;
                long totalUsers;
                var messageContent = "";
                switch (Prediction.Type)
                {
                    //Prediction is created
                    case PredictionType.EventCreated:
                        var endDate = Prediction.CreatedAt.Value.AddSeconds(Prediction.PredictionTime);
                        var endDateOffset = new DateTimeOffset(endDate).ToUnixTimeSeconds();
                        messageContent = "<@&1080237787174948936>";
                        embed.WithTitle($"Prediction Created: {Prediction.Title}");
                        embed.WithDescription($"Voting ends: <t:{endDateOffset}:R>");
                        embed.WithColor(Color.Green);
                        embed.AddField("Outcomes", string.Join("\n", Prediction.Outcomes.Select(p => $"{p.Title}")));
                        break;
                    //Predictiomn is locked
                    case PredictionType.EventUpdated when Prediction.Status == PredictionStatus.Locked:
                        embed.WithTitle($"Prediction Locked: {Prediction.Title}");
                        embed.WithColor(Color.Blue);
                        totalPoints = Prediction.Outcomes.Sum(p => p.TotalPoints);
                        totalUsers = Prediction.Outcomes.Sum(p => p.TotalUsers);
                        foreach (var outcome in Prediction.Outcomes)
                            embed.AddField($"{outcome.Title}",
                                $"Users: {outcome.TotalUsers:n0} {(outcome.TotalUsers/totalUsers)*100:n0}\n" +
                                $"Points: {outcome.TotalPoints:n0} {(outcome.TotalPoints/totalPoints)*100:n0}\n" +
                                $"Ratio: 1:{Math.Round((double)totalPoints / outcome.TotalPoints, 2)}");
                        break;
                    //Prediction was canceled
                    case PredictionType.EventUpdated when Prediction.Status == PredictionStatus.Canceled:
                        embed.WithTitle($"Prediction Canceled: {Prediction.Title}");
                        embed.WithColor(Color.Red);
                        break;
                    //Prediction has Ended
                    case PredictionType.EventUpdated when Prediction.Status == PredictionStatus.Resolved:
                        embed.WithTitle($"Prediction Ended: {Prediction.Title}");
                        embed.WithColor(Color.Gold);
                        //Get result
                        var winOutcome = Prediction.Outcomes.FirstOrDefault(p => p.Id == Prediction.WinningOutcomeId);
                        //Create field for winning outcome
                        totalPoints = Prediction.Outcomes.Sum(p => p.TotalPoints);
                        totalUsers = Prediction.Outcomes.Sum(p => p.TotalUsers);
                        var winRatio = (double)totalPoints / winOutcome.TotalPoints;
                        embed.AddField($"🎉 {winOutcome.Title} 🎉",
                            $"Users: {winOutcome.TotalUsers:n0} {(winOutcome.TotalUsers/totalUsers)*100:n0}\n" +
                            $"Points: {winOutcome.TotalPoints:n0} {(winOutcome.TotalPoints/totalPoints)*100:n0}\n" +
                            $"Ratio: 1:{Math.Round(winRatio, 2)}" +
                            "\n\n__Biggest Winners__\n" +
                            string.Join("\n", winOutcome.TopPredictors.OrderByDescending(p => p.Points).Take(5)
                                .Select(p =>
                                    $"{p.DisplayName} bet {p.Points:n0} and received {p.Points * winRatio:n0} points")));
                        //Create field for each loosing outcome
                        foreach (var outcome in Prediction.Outcomes.Where(o => o.Id != winOutcome.Id))
                            embed.AddField($"😭 {outcome.Title} 😭",
                                $"Users: {outcome.TotalUsers:n0} {(outcome.TotalUsers/totalUsers)*100:n0}\n" +
                                $"Points: {outcome.TotalPoints:n0} {(outcome.TotalPoints/totalPoints)*100:n0}\n" +
                                $"Ratio: 1:{Math.Round((double)totalPoints / outcome.TotalPoints, 2)}" +
                                "\n\n__Biggest Losers__\n" +
                                string.Join("\n", outcome.TopPredictors.OrderByDescending(p => p.Points).Take(5)
                                    .Select(p => $"{p.DisplayName} lost {p.Points:n0} points")));
                        break;
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
            });
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
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