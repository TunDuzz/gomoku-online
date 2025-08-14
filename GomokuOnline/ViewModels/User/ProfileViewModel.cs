namespace GomokuOnline.ViewModels.User
{
    public class ProfileViewModel
    {
        public int UserId { get; set; }
        public string Username { get; set; }
        public string Email { get; set; }
        public string? FullName { get; set; }
        public string? AvatarUrl { get; set; }

        // Statistics
        public int TotalWins { get; set; }
        public int TotalLosses { get; set; }
        public int TotalDraws { get; set; }
        public int TotalGames => TotalWins + TotalLosses + TotalDraws;
        public double WinRate => TotalGames > 0 ? (double)TotalWins / (TotalWins + TotalLosses) * 100 : 0;
        public int Rating { get; set; }

        // Recent activity
        public List<RecentGameInfo> RecentGames { get; set; } = new();
        public DateTime? LastLoginAt { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class RecentGameInfo
    {
        public int GameId { get; set; }
        public string OpponentUsername { get; set; }
        public string Result { get; set; } // Win, Loss, Draw
        public DateTime PlayedAt { get; set; }
        public int MovesCount { get; set; }
        public TimeSpan Duration { get; set; }
    }
}
