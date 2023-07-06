using Microsoft.EntityFrameworkCore;

namespace DougBot.Models;

public partial class DougBotContext : DbContext
{
    public DougBotContext()
    {
    }

    public DougBotContext(DbContextOptions<DougBotContext> options)
        : base(options)
    {
    }

    public virtual DbSet<Guild> Guilds { get; set; }

    public virtual DbSet<Youtube> Youtubes { get; set; }

    protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
    {
        optionsBuilder.UseNpgsql(Environment.GetEnvironmentVariable("CONNECTION_STRING"));
    }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Guild>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("guilds_pkey");

            entity.ToTable("guilds");

            entity.Property(e => e.Id)
                .HasMaxLength(255)
                .HasColumnName("id");
            entity.Property(e => e.DmReceiptChannel)
                .HasMaxLength(255)
                .HasColumnName("dm_receipt_channel");
            entity.Property(e => e.LogBlacklistChannels).HasColumnName("log_blacklist_channels");
            entity.Property(e => e.LogChannel)
                .HasMaxLength(255)
                .HasColumnName("log_channel");
            entity.Property(e => e.OpenAiChatForum)
                .HasMaxLength(255)
                .HasColumnName("open_ai_chat_forum");
            entity.Property(e => e.OpenAiToken)
                .HasMaxLength(255)
                .HasColumnName("open_ai_token");
            entity.Property(e => e.OpenAiUrl)
                .HasMaxLength(255)
                .HasColumnName("open_ai_url");
            entity.Property(e => e.ReactionFilterChannels).HasColumnName("reaction_filter_channels");
            entity.Property(e => e.ReactionFilterEmotes).HasColumnName("reaction_filter_emotes");
            entity.Property(e => e.ReportChannel)
                .HasMaxLength(255)
                .HasColumnName("report_channel");
            entity.Property(e => e.TwitchBlockedWords).HasColumnName("twitch_blocked_words");
            entity.Property(e => e.TwitchBotName)
                .HasMaxLength(255)
                .HasColumnName("twitch_bot_name");
            entity.Property(e => e.TwitchBotRefreshToken)
                .HasMaxLength(255)
                .HasColumnName("twitch_bot_refresh_token");
            entity.Property(e => e.TwitchChannelId)
                .HasMaxLength(255)
                .HasColumnName("twitch_channel_id");
            entity.Property(e => e.TwitchChannelName)
                .HasMaxLength(255)
                .HasColumnName("twitch_channel_name");
            entity.Property(e => e.TwitchChannelRefreshToken)
                .HasMaxLength(255)
                .HasColumnName("twitch_channel_refresh_token");
            entity.Property(e => e.TwitchClientId)
                .HasMaxLength(255)
                .HasColumnName("twitch_client_id");
            entity.Property(e => e.TwitchClientSecret)
                .HasMaxLength(255)
                .HasColumnName("twitch_client_secret");
            entity.Property(e => e.TwitchContainsBlock).HasColumnName("twitch_contains_block");
            entity.Property(e => e.TwitchEndsWithBlock).HasColumnName("twitch_ends_with_block");
        });

        modelBuilder.Entity<Youtube>(entity =>
        {
            entity.HasKey(e => e.Id).HasName("youtubes_pkey");

            entity.ToTable("youtubes");

            entity.Property(e => e.Id)
                .HasMaxLength(255)
                .HasColumnName("id");
            entity.Property(e => e.GuildId)
                .HasMaxLength(255)
                .HasColumnName("guild_id");
            entity.Property(e => e.LastVideoId)
                .HasMaxLength(255)
                .HasColumnName("last_video_id");
            entity.Property(e => e.MentionRole)
                .HasMaxLength(255)
                .HasColumnName("mention_role");
            entity.Property(e => e.PostChannel)
                .HasMaxLength(255)
                .HasColumnName("post_channel");

            entity.HasOne(d => d.Guild).WithMany(p => p.Youtubes)
                .HasForeignKey(d => d.GuildId)
                .OnDelete(DeleteBehavior.ClientSetNull)
                .HasConstraintName("youtubes_guild_id_fkey");
        });

        OnModelCreatingPartial(modelBuilder);
    }

    partial void OnModelCreatingPartial(ModelBuilder modelBuilder);
}