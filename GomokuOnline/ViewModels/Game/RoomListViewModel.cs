using GomokuOnline.Models.Entities;

namespace GomokuOnline.ViewModels.Game
{
    public class RoomListViewModel
    {
        public List<GameRoom> Rooms { get; set; } = new();
        public List<RoomInfo> PublicRooms { get; set; } = new();
        public List<RoomInfo> MyRooms { get; set; } = new();
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
        public int PageSize { get; set; } = 10;
        public string? SearchTerm { get; set; }
    }

    public class RoomInfo
    {
        public int RoomId { get; set; }
        public string RoomName { get; set; } = string.Empty;
        public string RoomCode { get; set; } = string.Empty;
        public string CreatedByUsername { get; set; } = string.Empty;
        public int CurrentPlayers { get; set; }
        public int MaxPlayers { get; set; }
        public int CurrentSpectators { get; set; }
        public int MaxSpectators { get; set; }
        public string Status { get; set; } = string.Empty; // Waiting, InProgress, etc.
        public DateTime CreatedAt { get; set; }
        public bool CanJoin { get; set; }
    }
}