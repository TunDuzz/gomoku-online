using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GomokuOnline.Models.Entities
{
    [Table("UserStats")]
    public class UserStats
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int TotalGames { get; set; } = 0;

        [Required]
        public int TotalWins { get; set; } = 0;

        [Required]
        public int TotalLosses { get; set; } = 0;

        [Required]
        public int TotalDraws { get; set; } = 0;

        [Required]
        public int Rating { get; set; } = 1200; // Điểm xếp hạng

        [Required]
        public int HighestRating { get; set; } = 1200;

        public DateTime? LastGameAt { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;
    }
} 