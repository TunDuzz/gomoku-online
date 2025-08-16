using GomokuOnline.Models.Entities;

namespace GomokuOnline.Repositories.Interfaces
{
    public interface IGameRepository : IRepository<Game>
    {
        Task<Game?> GetGameWithDetailsAsync(int gameId);
        Task<List<Game>> GetActiveGamesAsync();
        Task<List<Game>> GetUserGamesAsync(int userId, int page = 1, int pageSize = 10);
        Task<Game?> CreateGameAsync(int gameRoomId, int boardSize, int winCondition);
        Task<bool> SetWinnerAsync(int gameId, int winnerUserId);
        Task<bool> SetCurrentTurnAsync(int gameId, int currentTurnUserId);
        Task<bool> UpdateGameStatusAsync(int gameId, GameStatus status);
        Task<List<GameRoom>> GetAllRoomsAsync();
    }
}