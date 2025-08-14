using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GomokuOnline.Models.Entities
{
    [Table("Games")]
    public class Game
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int GameRoomId { get; set; }

        [Required]
        public int BoardSize { get; set; } = 15;

        [Required]
        public int WinCondition { get; set; } = 5;

        public DateTime StartedAt { get; set; } = DateTime.UtcNow;

        public DateTime? EndedAt { get; set; }

        [Required]
        public GameStatus Status { get; set; } = GameStatus.InProgress;

        public int? WinnerUserId { get; set; }

        public int? CurrentTurnUserId { get; set; }

        public int? TimeLimitSeconds { get; set; }

        public DateTime? LastMoveAt { get; set; }

        public int TotalMoves { get; set; } = 0;

        // Navigation properties
        [ForeignKey("GameRoomId")]
        public virtual GameRoom GameRoom { get; set; } = null!;

        [ForeignKey("WinnerUserId")]
        public virtual User? WinnerUser { get; set; }

        [ForeignKey("CurrentTurnUserId")]
        public virtual User? CurrentTurnUser { get; set; }

        public virtual ICollection<Move> Moves { get; set; } = new List<Move>();
        public virtual ICollection<GameState> GameStates { get; set; } = new List<GameState>();
    }

    public enum GameStatus
    {
        InProgress = 0,
        Completed = 1,
        Draw = 2,
        Cancelled = 3,
        Timeout = 4
    }
}
