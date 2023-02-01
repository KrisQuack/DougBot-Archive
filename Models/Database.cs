using Microsoft.EntityFrameworkCore;

namespace DougBot.Models;

public class Database
{
    public class DougBotContext : DbContext
    {
        public DbSet<Guild>? Guilds { get; set; }
        public DbSet<Queue>? Queues { get; set; }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            var accountEndpoint = Environment.GetEnvironmentVariable("ACCOUNT_ENDPOINT");
            var accountKey = Environment.GetEnvironmentVariable("ACCOUNT_KEY");
            var databaseName = Environment.GetEnvironmentVariable("DATABASE_NAME");
            optionsBuilder.UseCosmos(accountEndpoint,accountKey, databaseName);
        }
        
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Guild>()
                .ToContainer("Guilds")
                .HasPartitionKey(e => e.Id);
            modelBuilder.Entity<Guild>().OwnsMany(p => p.YoutubeSettings);
            
            modelBuilder.Entity<Queue>()
                .ToContainer("Queues")
                .HasPartitionKey(e => e.Id);
        }
    }
}