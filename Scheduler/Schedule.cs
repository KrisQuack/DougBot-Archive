using Discord.WebSocket;
using DougBot.Models;
using System.Text.Json;

namespace DougBot.Scheduler;

public class Schedule
{
    private readonly DiscordSocketClient _Client;

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
        {
            try
            {
                await Task.Delay(900000);
                ReactionFilter.Filter(_Client, 100);
                Queue.Create("Youtube", null, null, DateTime.UtcNow);
                Queue.Create("Forum", null, null, DateTime.UtcNow);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }

    private async Task MainQueue()
    {
        while (true)
        {
            try
            {
                await Task.Delay(1000);
                //Scheduled jobs
                await ReactionFilter.Filter(_Client, 5);
                //Load Queue
                var pendingQueues = Queue.GetDue();
                //Run items 
                foreach (var queue in pendingQueues)
                {
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
                        Console.WriteLine(ex);
                        throw;
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }
    }
}