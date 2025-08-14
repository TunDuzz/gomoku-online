using GomokuOnline.Models.Entities;

namespace GomokuOnline.Repositories.Interfaces
{
    public interface IUserRepository : IRepository<User>
    {
        Task<User?> GetByUsernameAsync(string username);
        Task<User?> GetByEmailAsync(string email);
        Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail);
        Task<bool> IsUsernameExistsAsync(string username);
        Task<bool> IsEmailExistsAsync(string email);
        Task<List<User>> GetTopPlayersByRatingAsync(int count = 10);
        Task UpdateStatisticsAsync(int userId, bool isWin, bool isDraw = false);
        Task UpdateLastLoginAsync(int userId);
    }
}