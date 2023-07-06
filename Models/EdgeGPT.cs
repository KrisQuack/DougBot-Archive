﻿using System.Diagnostics;
using System.Text.Json;

namespace DougBot.Models;

public class EdgeGpt
{
    public static EdgeGptResponse Run(string message, string style)
    {
        try
        {
            var responseJson = RunCommand("python3", $"Models/EdgeGPTPython.py \"{style}\" \"{message}\"");
            if (!responseJson.StartsWith("{")) throw new Exception(responseJson);
            var response = JsonSerializer.Deserialize<EdgeGptResponse>(responseJson);
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

    public class EdgeGptResponse
    {
        public string text { get; set; }
        public string author { get; set; }
        public EdgeGptSource[] sources { get; set; }
        public string sources_text { get; set; }
        public string[] suggestions { get; set; }
        public int messages_left { get; set; }
        public string adaptive_text { get; set; }
    }

    public class EdgeGptSource
    {
        public string providerDisplayName { get; set; }
        public string seeMoreUrl { get; set; }
        public string searchQuery { get; set; }
    }
}