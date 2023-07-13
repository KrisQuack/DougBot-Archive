using System.Diagnostics;
using System.Text.Json;

namespace DougBot.Models;

public class EdgeGpt
{
    public static string Run(string message, string style)
    {
        try
        {
            var response = RunCommand("python3", $"Models/EdgeGPTPython.py \"{style}\" \"{message}\"");
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