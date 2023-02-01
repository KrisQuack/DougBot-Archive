namespace DougBot.Models;

public class Queue
{
    public string? Id { get; set; }
    public string? Type { get; set; }
    public int? Priority { get; set; } = 1;
    public string? Keys { get; set; }
    public DateTime? DueAt { get; set; } = DateTime.UtcNow;
}