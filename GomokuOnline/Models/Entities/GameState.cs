using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GomokuOnline.Models.Entities
{
    [Table("GameStates")]
    public class GameState
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int GameId { get; set; }

        [Required]
        public int MoveNumber { get; set; } // Số nước đi đã thực hiện

        [Required]
        public string BoardState { get; set; } = string.Empty; // JSON string của trạng thái bàn cờ

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int? LastMoveId { get; set; } // ID của nước đi cuối cùng

        public string? GameStatus { get; set; } // Trạng thái ván cờ tại thời điểm này

        // Navigation properties
        [ForeignKey("GameId")]
        public virtual Game Game { get; set; } = null!;

        [ForeignKey("LastMoveId")]
        public virtual Move? LastMove { get; set; }
    }
}
