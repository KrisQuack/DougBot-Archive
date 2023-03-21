using DougBot.Models;
using TwitchLib.Api;
using TwitchLib.Api.Services;
using TwitchLib.Api.Services.Events.LiveStreamMonitor;

namespace DougBot.Twitch;

public class Twitch
{
    public static async Task RunClient()
    {
        try
        {
            Console.WriteLine("Twitch Initialized");
            //Load settings
            var settings = (await Guild.GetGuild("567141138021089308")).TwitchSettings;
            //Setup API
            var API = new TwitchAPI();
            var monitor = new LiveStreamMonitorService(API);
            monitor.SetChannelsByName(new List<string> { settings.ChannelName });
            monitor.OnStreamOnline += Monitor_OnStreamOnline;
            monitor.OnStreamOffline += Monitor_OnStreamOffline;
            monitor.Start();
            //Setup PubSub
            var pubSub = new PubSub().Initialize(API, settings.ChannelId);
            //Setup IRC anonymously
            new IRC().Initialize(API, "853660174", settings.BotName, settings.ChannelName);
            //Refresh token when expired
            while (true)
            {
                var botRefresh =
                    await API.Auth.RefreshAuthTokenAsync(settings.BotRefreshToken, settings.ClientSecret, settings.ClientId);
                var dougRefresh =
                    await API.Auth.RefreshAuthTokenAsync(settings.ChannelRefreshToken, settings.ClientSecret,
                        settings.ClientId);
                API.Settings.AccessToken = botRefresh.AccessToken;
                API.Settings.ClientId = settings.ClientId;
                pubSub.SendTopics(dougRefresh.AccessToken);
                //Wait 1 hour to refresh
                await Task.Delay(TimeSpan.FromHours(1));
            }
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
        }
    }

    private static void Monitor_OnStreamOnline(object? sender, OnStreamOnlineArgs Stream)
    {
        Console.WriteLine($"Stream Online: {Stream.Channel}");
        //Automate online ticker, ping, perhaps twitch things like disable emote only mode
    }

    private static void Monitor_OnStreamOffline(object? sender, OnStreamOfflineArgs Stream)
    {
        Console.WriteLine($"Stream Offline: {Stream.Channel}");
        //Automate delete ticker, perhaps twitch things like enable emote only mode for offline chat
    }
}