using GomokuOnline.Models.Entities;

namespace GomokuOnline.Services.Interfaces
{
    public interface IGameService
    {
        Task<Game?> CreateGameAsync(int gameRoomId, int boardSize, int winCondition);
        Task<bool> MakeMoveAsync(int gameId, int userId, int row, int column);
        Task<bool> CheckWinConditionAsync(int gameId, int row, int column, string symbol);
        Task<bool> IsValidMoveAsync(int gameId, int row, int column);
        Task<Game?> GetGameWithDetailsAsync(int gameId);
        Task<List<Game>> GetActiveGamesAsync();
        Task<List<Game>> GetUserGamesAsync(int userId, int page = 1, int pageSize = 10);
        Task<bool> SetWinnerAsync(int gameId, int winnerUserId);
        Task<bool> UpdateGameStatusAsync(int gameId, GameStatus status);
        Task<Move?> GetLastMoveAsync(int gameId);
        Task<List<GameRoom>> GetAllRoomsAsync();
        Task<List<GameRoom>> GetWaitingRoomsAsync();
    }
}
