using GomokuOnline.Models.Entities;
using GomokuOnline.ViewModels.User;

namespace GomokuOnline.Services.Interfaces
{
    public interface IAuthService
    {
        Task<(bool Success, string? ErrorMessage, User? User)> RegisterAsync(RegisterViewModel model);
        Task<(bool Success, string? ErrorMessage, User? User)> LoginAsync(LoginViewModel model);
        Task<bool> LogoutAsync(int userId);
        Task<UserSession?> CreateSessionAsync(int userId, string ipAddress, string userAgent);
        Task<bool> ValidateSessionAsync(string sessionToken);
        Task<User?> GetUserBySessionAsync(string sessionToken);
        string HashPassword(string password);
        string GenerateSessionToken();
        bool VerifyPassword(string password, string hash);
    }
}
