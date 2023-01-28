using System.Diagnostics;
using System.Text.Json;
using Discord.WebSocket;
using DougBot.Models;
using DougBot.Systems;

namespace DougBot.Scheduler;

public class Schedule
{
    private readonly DiscordSocketClient _Client;
    private readonly Dictionary<DateTime, int> _MainQueueTimeTracker = new();

    public Schedule(DiscordSocketClient client)
    {
        _Client = client;
        MainQueue();
        Long();
        Console.WriteLine("Scheduler System Initialized");
    }

    private async Task Long()
    {
        while (true)
            try
            {
                await Task.Delay(900000);
                ReactionFilter.Filter(_Client, 100);
                Queue.Create("Youtube", null, null, DateTime.UtcNow);
                Queue.Create("Forum", null, null, DateTime.UtcNow);
                //If MainQueueTimeTracker is empty not empty, log values and clear tracker
                if (_MainQueueTimeTracker.Count > 0)
                {
                    var settings = Setting.GetSettings();
                    var guild = _Client.Guilds.FirstOrDefault(x => x.Id.ToString() == settings.guildID);
                    var channel =
                        guild.Channels.FirstOrDefault(x => x.Id.ToString() == settings.logChannel) as SocketTextChannel;
                    channel.ModifyAsync(c => c.Topic =
                        $"Average Queue Time: {(int)_MainQueueTimeTracker.Values.Average()}ms\n" +
                        $"Minimum Queue Time: {_MainQueueTimeTracker.Values.Min()}ms\n" +
                        $"Maximum Queue Time: {_MainQueueTimeTracker.Values.Max()}ms\n" +
                        $"Total Queues: {_MainQueueTimeTracker.Count}"
                    );
                    _MainQueueTimeTracker.Clear();
                }
            }
            catch (Exception ex)
            {
                AuditLog.LogEvent(_Client, $"Error Occured: {ex.Message}",
                    false);
            }
    }

    private async Task MainQueue()
    {
        while (true)
            try
            {
                await Task.Delay(1000);
                var stopwatch = Stopwatch.StartNew();
                //Scheduled jobs
                await ReactionFilter.Filter(_Client, 5);
                //Load Queue
                var pendingQueues = Queue.GetDue();
                //Run items 
                foreach (var queue in pendingQueues)
                    try
                    {
                        await Task.Delay(100);
                        var param = new Dictionary<string, string>();
                        if (queue.Keys != null)
                            param = JsonSerializer.Deserialize<Dictionary<string, string>>(queue.Keys);

                        switch (queue.Type)
                        {
                            case "Forum":
                                await Forums.Clean(_Client);
                                break;
                            case "Youtube":
                                await Youtube.CheckYoutube(_Client);
                                break;
                            case "RemoveRole":
                                await Role.Remove(_Client,
                                    ulong.Parse(param["guildId"]),
                                    ulong.Parse(param["userId"]),
                                    ulong.Parse(param["roleId"]));
                                break;
                            case "AddRole":
                                await Role.Add(_Client,
                                    ulong.Parse(param["guildId"]),
                                    ulong.Parse(param["userId"]),
                                    ulong.Parse(param["roleId"]));
                                break;
                            case "RemoveReaction":
                                await Reaction.Remove(_Client,
                                    ulong.Parse(param["guildId"]),
                                    ulong.Parse(param["channelId"]),
                                    ulong.Parse(param["messageId"]),
                                    param["emoteName"]);
                                break;
                            case "SendMessage":
                                await Message.Send(_Client,
                                    ulong.Parse(param["guildId"]),
                                    ulong.Parse(param["channelId"]),
                                    param["message"],
                                    param["embedBuilders"]);
                                break;
                            case "SendDM":
                                await Message.SendDM(_Client,
                                    ulong.Parse(param["userId"]),
                                    ulong.Parse(param["SenderId"]),
                                    param["embedBuilders"]);
                                break;
                            case "FreshCheck":
                                await Onboarding.FreshmanCheck(_Client,
                                    ulong.Parse(param["guildId"]),
                                    ulong.Parse(param["userId"]));
                                break;
                            case "SetStatus":
                                await _Client.SetGameAsync(queue.Data);
                                break;
                        }

                        await Queue.Remove(queue);
                    }
                    catch (Exception ex)
                    {
                        AuditLog.LogEvent(_Client, $"Error Occured: {ex.Message}",
                            false);
                    }

                //log time taken
                stopwatch.Stop();
                _MainQueueTimeTracker.Add(DateTime.UtcNow, (int)stopwatch.ElapsedMilliseconds);
            }
            catch (Exception ex)
            {
                AuditLog.LogEvent(_Client, $"Error Occured: {ex.Message}",
                    false);
            }
    }
}