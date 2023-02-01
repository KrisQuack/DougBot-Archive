using System.Diagnostics;
using System.Text.Json;
using Discord.WebSocket;
using DougBot.Models;
using DougBot.Systems;

namespace DougBot.Scheduler;

public class Scheduler
{
    public static async Task Schedule(DiscordSocketClient Client)
    {
        while (true)
            try
            {
                await Task.Delay(1000);
                //Load Queue
                await using var db = new Database.DougBotContext();
                //Run items 
                foreach (var queue in db.Queues.Where(q => q.DueAt < DateTime.UtcNow).OrderBy(q => q.Priority))
                    try
                    {
                        //Run reaction filter
                        await ReactionFilter.Filter(Client);
                        //Run queue
                        var param = new Dictionary<string, string>();
                        if (queue.Keys != null)
                            param = JsonSerializer.Deserialize<Dictionary<string, string>>(queue.Keys);

                        switch (queue.Type)
                        {
                            case "RemoveRole":
                                await Role.Remove(Client,
                                    ulong.Parse(param["guildId"]),
                                    ulong.Parse(param["userId"]),
                                    ulong.Parse(param["roleId"]));
                                break;
                            case "AddRole":
                                await Role.Add(Client,
                                    ulong.Parse(param["guildId"]),
                                    ulong.Parse(param["userId"]),
                                    ulong.Parse(param["roleId"]));
                                break;
                            case "RemoveReaction":
                                await Reaction.Remove(Client,
                                    ulong.Parse(param["guildId"]),
                                    ulong.Parse(param["channelId"]),
                                    ulong.Parse(param["messageId"]),
                                    param["emoteName"]);
                                break;
                            case "SendMessage":
                                await Message.Send(Client,
                                    ulong.Parse(param["guildId"]),
                                    ulong.Parse(param["channelId"]),
                                    param["message"],
                                    param["embedBuilders"],
                                    bool.Parse(param["ping"]));
                                break;
                            case "SendDM":
                                await Message.SendDM(Client,
                                    ulong.Parse(param["guildId"]),
                                    ulong.Parse(param["userId"]),
                                    ulong.Parse(param["SenderId"]),
                                    param["embedBuilders"]);
                                break;
                            case "FreshCheck":
                                await Onboarding.FreshmanCheck(Client,
                                    ulong.Parse(param["guildId"]),
                                    ulong.Parse(param["userId"]));
                                break;
                            case "SetStatus":
                                await Client.SetGameAsync(param["status"]);
                                break;
                        }
                        db.Queues.Remove(queue);
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine(ex.Message);
                    }
                await db.SaveChangesAsync();
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex.Message);
            }
    }
}