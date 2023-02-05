using System.Text.Json;
using Microsoft.EntityFrameworkCore;

namespace DougBot.Models;

public class Queue
{
    public string? Id { get; set; }
    public string? Type { get; set; }
    public int? Priority { get; set; } = 1;
    public Dictionary<string,string>? Keys { get; set; }
    public DateTime? DueAt { get; set; } = DateTime.UtcNow;
    
    public Queue(string Type, int? Priority, Dictionary<string,string> Keys, DateTime? DueAt)
    {
        this.Id = Guid.NewGuid().ToString();
        this.Type = Type;
        this.Keys = Keys;
        if(Priority != null)
            this.Priority = Priority;
        if(DueAt != null)
            this.DueAt = DueAt;
    }

    public static async Task<List<Queue>> GetQueues()
    {
        await using var db = new Database.DougBotContext();
        return await db.Queues.ToListAsync();
    }
    
    public static async Task<List<Queue>> GetQueuesDue(int Take)
    {
        await using var db = new Database.DougBotContext();
        return await db.Queues.Where(q => q.DueAt < DateTime.UtcNow).OrderBy(q => q.Priority).Take(Take).ToListAsync();
    }

    public async Task Insert()
    {
        await using var db = new Database.DougBotContext();
        var similarItem = await db.Queues.Where(q => q.Keys == Keys && q.Type == Type).ToListAsync();
        if(!similarItem.Any())
        {
            db.Queues.Add(this);
            await db.SaveChangesAsync();
        }
    }

    public async Task Remove()
    {
        await using var db = new Database.DougBotContext();
        db.Queues.Remove(this);
        await db.SaveChangesAsync();
    }
}