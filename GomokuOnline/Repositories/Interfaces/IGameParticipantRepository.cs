using GomokuOnline.Models.Entities;
using GomokuOnline.Repositories.Implementations;

namespace GomokuOnline.Repositories.Interfaces
{
    public interface IGameParticipantRepository : IRepository<GameParticipant>
    {
        Task<List<GameParticipant>> GetGameParticipantsAsync(int gameId);
        Task<GameParticipant?> GetUserParticipationAsync(int gameId, int userId);
        Task<bool> AddParticipantAsync(int gameId, int userId, int typeId);
        Task<bool> RemoveParticipantAsync(int gameId, int userId);
        Task<int> GetParticipantsCountByTypeAsync(int gameId, int typeId);
    }
}