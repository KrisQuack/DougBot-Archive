using System.Diagnostics;
using System.Text.Json;
using System.Text.RegularExpressions;
using Discord;
using DougBot.Systems;

namespace DougBot.Models;

public class EdgeGpt
{
    public static async Task<string> Run(string message, string style, string guildId)
    {
        try
        {
            var response = RunCommand("python3", $"Models/EdgeGPTPython.py \"{style}\" \"{message}\"");
            //Audit log response
            //Create fields for audit log
            var fields = new List<EmbedFieldBuilder>
            {
                new()
                {
                    Name = "Response",
                    Value = response,
                    IsInline = false
                }
            };
            //Author
            var author = new EmbedAuthorBuilder
            {
                Name = "EdgeGPT"
            };
            //Send audit log
            await AuditLog.LogEvent("AI Chat", guildId, Color.Blue, fields, author);
            //Remove lines containing "BingChat|DEBUG" and get the last entry splitting by \n------\n
            response = response.Split("\n------\n").Last();
            response = Regex.Replace(response, @"BingChat\|DEBUG.*\n", "");
            return response;
        }
        catch (Exception e)
        {
            Console.WriteLine(e);
            throw;
        }
    }

    private static string RunCommand(string command, string args)
    {
        var process = new Process
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            }
        };
        process.Start();
        var output = process.StandardOutput.ReadToEnd();
        var error = process.StandardError.ReadToEnd();
        process.WaitForExit();

        return string.IsNullOrEmpty(error) ? output : error;
    }
}