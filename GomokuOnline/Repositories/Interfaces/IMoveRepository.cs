using GomokuOnline.Models.Entities;
using GomokuOnline.Repositories.Implementations;

namespace GomokuOnline.Repositories.Interfaces
{
    public interface IMoveRepository : IRepository<Move>
    {
        Task<List<Move>> GetGameMovesAsync(int gameId);
        Task<Move?> GetLastMoveAsync(int gameId);
        Task<int> GetNextSequenceNumberAsync(int gameId);
        Task<bool> IsMoveValidAsync(int gameId, int x, int y);
        Task<Move> AddMoveAsync(int gameId, int userId, int x, int y, int timeSpentSeconds = 0);
    }
}