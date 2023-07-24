using Discord;
using DougBot.Scheduler;
using TwitchLib.PubSub;
using TwitchLib.PubSub.Enums;
using TwitchLib.PubSub.Events;

namespace DougBot.Systems.Twitch;

public class PubSub
{
    public TwitchPubSub Create()
    {
        var client = new TwitchPubSub();
        //Main events
        client.OnListenResponse += OnListenResponse;
        client.OnPubSubServiceClosed += OnPubSubServiceClosed;
        client.OnPubSubServiceError += OnPubSubServiceError;
        //Listeners
        client.OnPrediction += PubSub_OnPrediction;
        client.OnChannelPointsRewardRedeemed += PubSub_OnChannelPointsRewardRedeemed;
        return client;
    }

    private void PubSub_OnPrediction(object? sender, OnPredictionArgs prediction)
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
                if (prediction.Type == PredictionType.EventCreated)
                {
                    var endDate = prediction.CreatedAt.Value.AddSeconds(prediction.PredictionTime);
                    var endDateOffset = new DateTimeOffset(endDate).ToUniversalTime().ToUnixTimeSeconds();
                    messageContent = "<@&1080237787174948936>";
                    embed.WithTitle($"Prediction Created: {prediction.Title}");
                    embed.WithDescription($"Voting ends: <t:{endDateOffset}:R>");
                    embed.WithColor(Color.Green);
                    embed.AddField("Outcomes", string.Join("\n", prediction.Outcomes.Select(p => $"{p.Title}")));
                }
                //Canceled
                else if (prediction.Type == PredictionType.EventUpdated &&
                         prediction.Status == PredictionStatus.Canceled)
                {
                    embed.WithTitle($"Prediction Canceled: {prediction.Title}");
                    embed.WithColor(Color.Red);
                }
                //Locked or Resolved
                else if (prediction.Type == PredictionType.EventUpdated &&
                         (prediction.Status == PredictionStatus.Locked ||
                          prediction.Status == PredictionStatus.Resolved))
                {
                    var isResolved = prediction.Status == PredictionStatus.Resolved;
                    embed.WithTitle($"Prediction {(isResolved ? "Ended" : "Locked")}: {prediction.Title}");
                    embed.WithColor(isResolved ? Color.Orange : Color.Blue);
                    var winOutcome = prediction.Outcomes.FirstOrDefault(p => p.Id == prediction.WinningOutcomeId);
                    var totalPoints = prediction.Outcomes.Sum(p => p.TotalPoints);
                    var totalUsers = prediction.Outcomes.Sum(p => p.TotalUsers);
                    //Create field for each loosing outcome
                    foreach (var outcome in prediction.Outcomes)
                    {
                        var isWinning = winOutcome != null && outcome.Id == prediction.WinningOutcomeId;
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

                //Check the embed is not empty
                if (string.IsNullOrEmpty(embed.Title)) return;
                //Send message
                await SendMessageJob.Queue("567141138021089308", "1070317311505997864",
                    new List<EmbedBuilder> { embed }, DateTime.UtcNow, messageContent, true);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        });
    }


    private void PubSub_OnChannelPointsRewardRedeemed(object sender, OnChannelPointsRewardRedeemedArgs redemptionArg)
    {
        _ = Task.Run(async () =>
        {
            var redemption = redemptionArg.RewardRedeemed.Redemption;
            var reward = redemption.Reward;
            var redeemedUser = redemption.User;
            //Notify mods of new redemption
            if (redemption.Status == "UNFULFILLED")
                if (reward.Title.Contains("Minecraft Server"))
                    Twitch.Irc.SendMessage("dougdoug",
                        $"@{redeemedUser.DisplayName} Thanks for redeeming Minecraft access, Please join the discord and complete this form https://forms.gle/oouvNweqqBFZ8DtD9");
        });
    }

    private void OnPubSubServiceError(object sender, OnPubSubServiceErrorArgs e)
    {
        Console.WriteLine($"[General/Warning] {DateTime.UtcNow:HH:mm:ss} PubSub {e.Exception}");
    }

    private void OnPubSubServiceClosed(object sender, EventArgs e)
    {
        Console.WriteLine($"[General/Warning] {DateTime.UtcNow:HH:mm:ss} PubSub Closed");
    }

    private void OnListenResponse(object sender, OnListenResponseArgs e)
    {
        if (!e.Successful) Console.WriteLine($"[General/Warning] {DateTime.UtcNow:HH:mm:ss} PubSub {e}");
    }
}