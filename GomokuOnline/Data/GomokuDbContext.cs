using Microsoft.EntityFrameworkCore;
using GomokuOnline.Models.Entities;

namespace GomokuOnline.Data
{
    public class GomokuDbContext : DbContext
    {
        public GomokuDbContext(DbContextOptions<GomokuDbContext> options) : base(options)
        {
        }

        // DbSets
        public DbSet<User> Users { get; set; }
        public DbSet<GameRoom> GameRooms { get; set; }
        public DbSet<GameParticipant> GameParticipants { get; set; }
        public DbSet<Game> Games { get; set; }
        public DbSet<Move> Moves { get; set; }
        public DbSet<GameState> GameStates { get; set; }
        public DbSet<ChatMessage> ChatMessages { get; set; }
        public DbSet<UserSession> UserSessions { get; set; }
        public DbSet<UserStats> UserStats { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User configuration
            modelBuilder.Entity<User>(entity =>
            {
                entity.HasIndex(e => e.Username).IsUnique();
                entity.HasIndex(e => e.Email).IsUnique();
                entity.Property(e => e.Username).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
            });

            // GameRoom configuration
            modelBuilder.Entity<GameRoom>(entity =>
            {
                entity.Property(e => e.Name).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Description).HasMaxLength(500);
                entity.Property(e => e.Password).HasMaxLength(50);
                entity.HasOne(e => e.CreatedByUser)
                      .WithMany()
                      .HasForeignKey(e => e.CreatedByUserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // GameParticipant configuration
            modelBuilder.Entity<GameParticipant>(entity =>
            {
                entity.HasOne(e => e.User)
                      .WithMany(u => u.GameParticipants)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.GameRoom)
                      .WithMany(r => r.Participants)
                      .HasForeignKey(e => e.GameRoomId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Unique constraint: một user chỉ có thể tham gia một phòng một lần
                entity.HasIndex(e => new { e.UserId, e.GameRoomId }).IsUnique();
            });

            // Game configuration
            modelBuilder.Entity<Game>(entity =>
            {
                entity.HasOne(e => e.GameRoom)
                      .WithMany(r => r.Games)
                      .HasForeignKey(e => e.GameRoomId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.WinnerUser)
                      .WithMany()
                      .HasForeignKey(e => e.WinnerUserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.CurrentTurnUser)
                      .WithMany()
                      .HasForeignKey(e => e.CurrentTurnUserId)
                      .OnDelete(DeleteBehavior.Restrict);
            });

            // Move configuration
            modelBuilder.Entity<Move>(entity =>
            {
                entity.HasOne(e => e.Game)
                      .WithMany(g => g.Moves)
                      .HasForeignKey(e => e.GameId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.User)
                      .WithMany()
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Index for performance
                entity.HasIndex(e => new { e.GameId, e.MoveNumber });
            });

            // GameState configuration
            modelBuilder.Entity<GameState>(entity =>
            {
                entity.HasOne(e => e.Game)
                      .WithMany(g => g.GameStates)
                      .HasForeignKey(e => e.GameId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.LastMove)
                      .WithMany()
                      .HasForeignKey(e => e.LastMoveId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Index for performance
                entity.HasIndex(e => new { e.GameId, e.MoveNumber });
            });

            // ChatMessage configuration
            modelBuilder.Entity<ChatMessage>(entity =>
            {
                entity.Property(e => e.Content).IsRequired().HasMaxLength(1000);

                entity.HasOne(e => e.User)
                      .WithMany(u => u.ChatMessages)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Restrict);

                entity.HasOne(e => e.GameRoom)
                      .WithMany(r => r.ChatMessages)
                      .HasForeignKey(e => e.GameRoomId)
                      .OnDelete(DeleteBehavior.Cascade);

                entity.HasOne(e => e.ReplyToMessage)
                      .WithMany(m => m.Replies)
                      .HasForeignKey(e => e.ReplyToMessageId)
                      .OnDelete(DeleteBehavior.Restrict);

                // Index for performance
                entity.HasIndex(e => new { e.GameRoomId, e.CreatedAt });
            });

            // UserSession configuration
            modelBuilder.Entity<UserSession>(entity =>
            {
                entity.Property(e => e.SessionToken).IsRequired().HasMaxLength(255);
                entity.Property(e => e.IpAddress).IsRequired().HasMaxLength(45);

                entity.HasOne(e => e.User)
                      .WithMany(u => u.UserSessions)
                      .HasForeignKey(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Index for performance
                entity.HasIndex(e => e.SessionToken).IsUnique();
                entity.HasIndex(e => new { e.UserId, e.IsActive });
            });

            // UserStats configuration
            modelBuilder.Entity<UserStats>(entity =>
            {
                entity.HasOne(e => e.User)
                      .WithOne(u => u.UserStats)
                      .HasForeignKey<UserStats>(e => e.UserId)
                      .OnDelete(DeleteBehavior.Cascade);

                // Index for performance
                entity.HasIndex(e => e.Rating);
                entity.HasIndex(e => e.TotalWins);
            });
        }
    }
}
