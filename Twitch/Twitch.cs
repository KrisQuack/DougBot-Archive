using DougBot.Models;
using TwitchLib.Api;
using TwitchLib.Api.Auth;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events.LiveStreamMonitor;
using TwitchLib.Client;
using TwitchLib.Client.Models;
using TwitchLib.PubSub;

namespace DougBot.Twitch;

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
            //Load settings
            var settings = (await Guild.GetGuild("567141138021089308")).TwitchSettings;
            //Setup API
            Api = new TwitchAPI();
            var monitor = new LiveStreamMonitorService(Api);
            monitor.SetChannelsByName(new List<string> { settings.ChannelName });
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
            Irc = new Irc().Create(settings.ChannelName);
            Irc.OnConnected += (sender, e) => { Irc.JoinChannel(settings.ChannelName); };
            //Refresh token when expired
            while (true)
                try
                {
                    //Refresh tokens
                    botRefresh =
                        await Api.Auth.RefreshAuthTokenAsync(settings.BotRefreshToken, settings.ClientSecret,
                            settings.ClientId);
                    dougRefresh =
                        await Api.Auth.RefreshAuthTokenAsync(settings.ChannelRefreshToken, settings.ClientSecret,
                            settings.ClientId);
                    Api.Settings.AccessToken = botRefresh.AccessToken;
                    Api.Settings.ClientId = settings.ClientId;
                    //Connect IRC
                    var credentials = new ConnectionCredentials(settings.BotName, Api.Settings.AccessToken,
                        disableUsernameCheck: true);
                    Irc.SetConnectionCredentials(credentials);
                    Irc.Connect();
                    //Update PubSub
                    PubSub.Connect();
                    PubSub.ListenToChannelPoints(settings.ChannelId);
                    PubSub.ListenToPredictions(settings.ChannelId);
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