using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GomokuOnline.Models.Entities
{
    [Table("GameParticipants")]
    public class GameParticipant
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int GameRoomId { get; set; }

        [Required]
        public ParticipantType Type { get; set; } = ParticipantType.Player;

        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        public DateTime? LeftAt { get; set; }

        public bool IsReady { get; set; } = false;

        public int? PlayerOrder { get; set; } // Thứ tự người chơi (1, 2, 3, 4...)

        public string? PlayerColor { get; set; } // Màu quân cờ (X, O, etc.)

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("GameRoomId")]
        public virtual GameRoom GameRoom { get; set; } = null!;
    }

    public enum ParticipantType
    {
        Player = 0,
        Spectator = 1,
        Moderator = 2
    }
}