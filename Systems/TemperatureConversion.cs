using Discord.WebSocket;
using Discord;
using Fernandezja.ColorHashSharp;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Text.RegularExpressions;
using DougBot.Models;
using DougBot.Scheduler;

namespace DougBot.Systems
{
    public static class TemperatureConversion
    {
        public static async Task Monitor()
        {
            Program.Client.MessageReceived += MessageReceivedHandler;
            Console.WriteLine("DMRelay Initialized");
        }

        private static Task MessageReceivedHandler(SocketMessage message)
        {
            _ = Task.Run(async () =>
            {
                try
                {
                    //If the message is not from guild 1079506249139363970 then return
                    var guild = (message.Channel as SocketGuildChannel).Guild.Id;
                    if (guild != 290611616586924033)
                        return;
                    //Get message text
                    var messageText = message.CleanContent.ToLower();
                    // Define the regex pattern with word boundaries
                    var pattern = @"(-?\d+(\.\d+)?)([cf])";
                    // Create a regex object with the pattern
                    var regex = new Regex(pattern);
                    // Check if the input matches the regex
                    if (regex.IsMatch(messageText))
                    {
                        var regexMatch = regex.Match(messageText).Value;
                        if (!regexMatch.EndsWith('c') && !regexMatch.EndsWith('f'))
                            return;
                        // Extract the number and the unit from the input
                        var number = double.Parse(regexMatch.TrimEnd('c', 'f'));
                        var unit = regexMatch.EndsWith('c') ? 'c' : 'f';
                        // Convert the number to the other unit using the formulas
                        // C = (F - 32) * 5/9
                        // F = C * 9/5 + 32
                        if (unit == 'c')
                        {
                            number = (int)Math.Round(number * 9.0 / 5 + 32);
                            unit = 'f';
                        }
                        else if (unit == 'f')
                        {
                            number = (int)Math.Round((number - 32) * 5.0 / 9);
                            unit = 'c';
                        }
                        //Load Dictionary
                        var numbersDict = new Dictionary<char, string>()
                        {
                            {'0', "0️⃣"},
                            {'1', "1️⃣"},
                            {'2', "2️⃣"},
                            {'3', "3️⃣"},
                            {'4', "4️⃣"},
                            {'5', "5️⃣"},
                            {'6', "6️⃣"},
                            {'7', "7️⃣"},
                            {'8', "8️⃣"},
                            {'9', "9️⃣"},
                            {'-', "➖"}
                        };
                        //React to the message using the numbers in the format 1️⃣ 2️⃣ and the unit 🇨 and 🇫
                        var unitEmoji = unit == 'c' ? "🇨" : "🇫";
                        foreach (var numberChar in number.ToString())
                        {
                            await AddReactionJob.Queue(guild.ToString(), message.Channel.Id.ToString(), message.Id.ToString(), numbersDict[numberChar], DateTime.UtcNow);
                        }
                        await AddReactionJob.Queue(guild.ToString(), message.Channel.Id.ToString(), message.Id.ToString(), $"{(unit == 'c' ? "🇨" : "🇫")}", DateTime.UtcNow);
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"[General/Warning] {DateTime.UtcNow:HH:mm:ss} TemperatureConversion {ex}");
                }
            });
            return Task.CompletedTask;
        }
    }
}