using System.Diagnostics;

namespace DougBot.Models;

public class EdgeGPT
{
    public static EdgeGPTResponse Run(string message, string style)
    {
        var responseJson = RunCommand("python3", $"Models/EdgeGPTPython.py \"{style}\" \"{message}\"");
        if (!responseJson.Contains("EdgeGPT.exceptions"))
        {
            var response = System.Text.Json.JsonSerializer.Deserialize<EdgeGPTResponse>(responseJson);
            return response;
        }
        throw new Exception(responseJson);
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
    
    public class EdgeGPTResponse
    {
        public string text { get; set; }
        public string author { get; set; }
        public object[] sources { get; set; }
        public string sources_text { get; set; }
        public string[] suggestions { get; set; }
        public int messages_left { get; set; }
        public string adaptive_text { get; set; }
    }
}