using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GomokuOnline.Models.Entities
{
    [Table("GameRooms")]
    public class GameRoom
    {
        [Key]
        public int Id { get; set; }

        [Required]
        [StringLength(100)]
        public string Name { get; set; } = string.Empty;

        [StringLength(500)]
        public string? Description { get; set; }

        [Required]
        public int CreatedByUserId { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? StartedAt { get; set; }

        public DateTime? EndedAt { get; set; }

        [Required]
        public RoomStatus Status { get; set; } = RoomStatus.Waiting;

        [Required]
        public int MaxPlayers { get; set; } = 2;

        [Required]
        public int BoardSize { get; set; } = 15;

        [Required]
        public int WinCondition { get; set; } = 5; // Số quân cờ liên tiếp để thắng

        public bool IsPrivate { get; set; } = false;

        [StringLength(50)]
        public string? Password { get; set; }

        public int? TimeLimitMinutes { get; set; } // Thời gian giới hạn cho mỗi lượt

        // Navigation properties
        [ForeignKey("CreatedByUserId")]
        public virtual User CreatedByUser { get; set; } = null!;

        public virtual ICollection<GameParticipant> Participants { get; set; } = new List<GameParticipant>();
        public virtual ICollection<Game> Games { get; set; } = new List<Game>();
        public virtual ICollection<ChatMessage> ChatMessages { get; set; } = new List<ChatMessage>();
    }

    public enum RoomStatus
    {
        Waiting = 0,
        Playing = 1,
        Finished = 2,
        Cancelled = 3
    }
}
