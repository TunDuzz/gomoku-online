using GameEntity = GomokuOnline.Models.Entities.Game;

namespace GomokuOnline.ViewModels.Game
{
    public class GameViewModel
    {
        // Dashboard properties
        public List<GameEntity> ActiveGames { get; set; } = new();
        public List<GameEntity> UserGames { get; set; } = new();

        // Single game properties
        public int GameId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string RoomCode { get; set; } = string.Empty;
        public int BoardSize { get; set; }
        public int WinCondition { get; set; }
        public string GameState { get; set; } = string.Empty;

        // Players info
        public PlayerInfo? Player1 { get; set; }
        public PlayerInfo? Player2 { get; set; }
        public List<SpectatorInfo> Spectators { get; set; } = new();

        // Current game state
        public string? CurrentPlayerTurn { get; set; }
        public int[,] Board { get; set; } = new int[15, 15];
        public List<MoveInfo> MoveHistory { get; set; } = new();

        // Chat messages
        public List<ChatMessageInfo> ChatMessages { get; set; } = new();

        // User's role in this game
        public string UserRole { get; set; } = string.Empty; // Player1, Player2, Spectator
        public bool CanMakeMove { get; set; }

        public DateTime? StartedAt { get; set; }
        public DateTime? EndedAt { get; set; }
        public string? WinnerUsername { get; set; }
    }

    public class PlayerInfo
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public int Rating { get; set; }
        public int TotalWins { get; set; }
        public int TotalLosses { get; set; }
        public bool IsOnline { get; set; }
    }

    public class SpectatorInfo
    {
        public int UserId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public DateTime JoinedAt { get; set; }
    }

    public class MoveInfo
    {
        public int MoveId { get; set; }
        public int SequenceNumber { get; set; }
        public int PositionX { get; set; }
        public int PositionY { get; set; }
        public string Username { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
        public int TimeSpentSeconds { get; set; }
    }

    public class ChatMessageInfo
    {
        public int MessageId { get; set; }
        public string Username { get; set; } = string.Empty;
        public string? AvatarUrl { get; set; }
        public string MessageContent { get; set; } = string.Empty;
        public string MessageType { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
    }
}