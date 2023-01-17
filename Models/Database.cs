using Microsoft.EntityFrameworkCore;

namespace DougBot.Models;

public class Database
{
    public class DougBotContext : DbContext
    {
        public DbSet<Setting> Settings { get; set; }
        public DbSet<Queue> Queues { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var host = Environment.GetEnvironmentVariable("DATABASE_HOST");
            var password = Environment.GetEnvironmentVariable("DATABASE_PASSWORD");
            optionsBuilder.UseNpgsql($"Host={host};Database=DougBot;Username=postgres;Password={password}");
        }
    }
}