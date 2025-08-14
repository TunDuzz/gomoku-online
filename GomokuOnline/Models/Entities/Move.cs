using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GomokuOnline.Models.Entities
{
    [Table("Moves")]
    public class Move
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int GameId { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int Row { get; set; }

        [Required]
        public int Column { get; set; }

        [Required]
        public string Symbol { get; set; } = string.Empty; // X, O, etc.

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public int MoveNumber { get; set; } // Thứ tự nước đi trong ván

        public bool IsValid { get; set; } = true;

        public string? Comment { get; set; } // Ghi chú về nước đi

        // Navigation properties
        [ForeignKey("GameId")]
        public virtual Game Game { get; set; } = null!;

        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
}