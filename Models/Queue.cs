namespace DougBot.Models;

public class Queue
{
    public int Id { get; set; }
    public string Type { get; set; }
    public string? Data { get; set; }
    public string? Keys { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime DueAt { get; set; }

    public static async Task Create(string type, string data, string keys, DateTime dueAt)
    {
        await using var db = new Database.DougBotContext();
        //Check does not already exist
        if (!db.Queues.Any(q => q.Keys == keys && q.Type == type))
        {
            db.Queues.Add(new Queue
            {
                Type = type,
                Data = data,
                Keys = keys,
                CreatedAt = DateTime.UtcNow,
                DueAt = dueAt
            });
            await db.SaveChangesAsync();
        }
        else
        {
            var queue = db.Queues.First(q => q.Keys == keys && q.Type == type);
            queue.DueAt = dueAt;
            await db.SaveChangesAsync();
        }
    }

    public static List<Queue> GetAll()
    {
        using var db = new Database.DougBotContext();
        return db.Queues.ToList();
    }

    public static List<Queue> GetDue()
    {
        using var db = new Database.DougBotContext();
        return db.Queues.Where(c => c.DueAt < DateTime.UtcNow).ToList();
    }

    public static async Task Remove(Queue queue)
    {
        await using var db = new Database.DougBotContext();
        db.Queues.Remove(queue);
        await db.SaveChangesAsync();
    }
}