using GomokuOnline.Models.Entities;
using GomokuOnline.Repositories.Implementations;

namespace GomokuOnline.Repositories.Interfaces
{
    public interface IGameRoomRepository : IRepository<GameRoom>
    {
        Task<List<GameRoom>> GetPublicRoomsAsync(int page = 1, int pageSize = 10, string? searchTerm = null);
        Task<List<GameRoom>> GetUserRoomsAsync(int userId);
        Task<GameRoom?> GetByRoomCodeAsync(string roomCode);
        Task<string> GenerateUniqueRoomCodeAsync();
        Task<bool> IsRoomNameExistsAsync(string roomName, int? excludeRoomId = null);
    }
}