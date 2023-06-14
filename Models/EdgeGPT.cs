using System.Diagnostics;
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
        var process = new Process()
        {
            StartInfo = new ProcessStartInfo
            {
                FileName = command,
                Arguments = args,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true,
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
        public string Text { get; set; }
        public string Author { get; set; }
        public object[] Sources { get; set; }
        public string SourcesText { get; set; }
        public string[] Suggestions { get; set; }
        public int MessagesLeft { get; set; }
        public string AdaptiveText { get; set; }
    }
}