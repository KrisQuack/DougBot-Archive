using TwitchLib.Api;
using TwitchLib.Api.Auth;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events.LiveStreamMonitor;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.PubSub;

namespace DougBot.Systems.Twitch;

public class Twitch
{
    public static TwitchAPI Api { get; private set; }
    public static TwitchClient Irc { get; private set; }
    private static TwitchPubSub PubSub { get; set; }

    public async Task RunClient()
    {
        try
        {
            Console.WriteLine("Twitch Initialized");
            //Setup API
            Api = new TwitchAPI();
            var monitor = new LiveStreamMonitorService(Api);
            monitor.SetChannelsByName(new List<string> { ConfigurationService.Instance.TwitchChannelName });
            monitor.OnStreamOnline += Monitor_OnStreamOnline;
            monitor.OnStreamOffline += Monitor_OnStreamOffline;
            monitor.Start();
            //Setup tokens
            RefreshResponse dougRefresh = null;
            RefreshResponse botRefresh = null;
            //Setup PubSub
            PubSub = new PubSub().Create();
            PubSub.OnPubSubServiceConnected += (sender, e) =>
            {
                while (dougRefresh == null) Task.Delay(1000);
                PubSub.SendTopics(dougRefresh.AccessToken);
                Console.WriteLine($"[General/Info] {DateTime.UtcNow:HH:mm:ss} PubSub Connected");
            };
            //Setup IRC anonymously
            Irc = new Irc().Create(ConfigurationService.Instance.TwitchChannelName);
            Irc.OnConnected += (sender, e) => { Irc.JoinChannel(ConfigurationService.Instance.TwitchChannelName); };
            //Refresh token when expired
            while (true)
                try
                {
                    //Refresh tokens
                    botRefresh =
                        await Api.Auth.RefreshAuthTokenAsync(ConfigurationService.Instance.TwitchBotRefreshToken,
                            ConfigurationService.Instance.TwitchClientSecret,
                            ConfigurationService.Instance.TwitchClientId);
                    dougRefresh =
                        await Api.Auth.RefreshAuthTokenAsync(ConfigurationService.Instance.TwitchChannelRefreshToken,
                            ConfigurationService.Instance.TwitchClientSecret,
                            ConfigurationService.Instance.TwitchClientId);
                    Api.Settings.AccessToken = botRefresh.AccessToken;
                    Api.Settings.ClientId = ConfigurationService.Instance.TwitchClientId;
                    //Connect IRC
                    var credentials = new ConnectionCredentials(ConfigurationService.Instance.TwitchBotName,
                        Api.Settings.AccessToken,
                        disableUsernameCheck: true);
                    Irc.SetConnectionCredentials(credentials);
                    Irc.Connect();
                    //Update PubSub
                    PubSub.Connect();
                    PubSub.ListenToChannelPoints(ConfigurationService.Instance.TwitchChannelId);
                    PubSub.ListenToPredictions(ConfigurationService.Instance.TwitchChannelId);
                    //Get the lowest refresh time
                    var refreshTime = botRefresh.ExpiresIn < dougRefresh.ExpiresIn
                        ? botRefresh.ExpiresIn
                        : dougRefresh.ExpiresIn;
                    Console.WriteLine(
                        $"[General/Info] {DateTime.UtcNow:HH:mm:ss} Next Twitch refresh in {refreshTime} seconds at {DateTime.UtcNow.AddSeconds(refreshTime):HH:mm}");
                    await Task.Delay((refreshTime - 1800) * 1000);
                    //Disconnected
                    PubSub.Disconnect();
                    Irc.Disconnect();
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[General/Warning] {DateTime.UtcNow:HH:mm:ss} Error refreshing tokens: {ex}");
                    await Task.Delay(60000);
                }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private void Monitor_OnStreamOnline(object? sender, OnStreamOnlineArgs stream)
    {
        Console.WriteLine($"Stream Online: {stream.Channel}");
        //Automate online ticker, ping, perhaps twitch things like disable emote only mode
    }

    private void Monitor_OnStreamOffline(object? sender, OnStreamOfflineArgs stream)
    {
        Console.WriteLine($"Stream Offline: {stream.Channel}");
        //Automate delete ticker, perhaps twitch things like enable emote only mode for offline chat
    }
}