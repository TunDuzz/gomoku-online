using System.Security.Cryptography;
using System.Text;
using Microsoft.EntityFrameworkCore;
using GomokuOnline.Data;
using GomokuOnline.Models.Entities;
using GomokuOnline.Repositories.Interfaces;
using GomokuOnline.Services.Interfaces;
using GomokuOnline.ViewModels.User;

namespace GomokuOnline.Services.Implementations
{
    public class AuthService : IAuthService
    {
        private readonly IUserRepository _userRepository;
        private readonly GomokuDbContext _context;

        public AuthService(IUserRepository userRepository, GomokuDbContext context)
        {
            _userRepository = userRepository;
            _context = context;
        }

        public async Task<(bool Success, string? ErrorMessage, User? User)> RegisterAsync(RegisterViewModel model)
        {
            // Check if username exists
            if (await _userRepository.IsUsernameExistsAsync(model.Username))
            {
                return (false, "Tên đăng nhập đã tồn tại", null);
            }

            // Check if email exists
            if (await _userRepository.IsEmailExistsAsync(model.Email))
            {
                return (false, "Email đã được sử dụng", null);
            }

            // Create user
            var passwordHash = HashPassword(model.Password);

            var user = new User
            {
                Username = model.Username,
                Email = model.Email,
                PasswordHash = passwordHash,
                FullName = model.FullName,
                CreatedAt = DateTime.UtcNow,
                IsActive = true
            };

            try
            {
                await _userRepository.AddAsync(user);
                
                // Create UserStats for the new user
                var userStats = new UserStats
                {
                    UserId = user.Id,
                    TotalGames = 0,
                    TotalWins = 0,
                    TotalLosses = 0,
                    TotalDraws = 0,
                    Rating = 1200,
                    HighestRating = 1200,
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };
                
                _context.UserStats.Add(userStats);
                await _context.SaveChangesAsync();
                
                return (true, null, user);
            }
            catch (Exception ex)
            {
                return (false, "Đã xảy ra lỗi khi tạo tài khoản", null);
            }
        }

        public async Task<(bool Success, string? ErrorMessage, User? User)> LoginAsync(LoginViewModel model)
        {
            var user = await _userRepository.GetByUsernameOrEmailAsync(model.UsernameOrEmail);

            if (user == null)
            {
                return (false, "Tên đăng nhập hoặc mật khẩu không đúng", null);
            }

            if (!VerifyPassword(model.Password, user.PasswordHash))
            {
                return (false, "Tên đăng nhập hoặc mật khẩu không đúng", null);
            }

            // Update last login
            await _userRepository.UpdateLastLoginAsync(user.Id);

            return (true, null, user);
        }

        public async Task<bool> LogoutAsync(int userId)
        {
            var sessions = await _context.UserSessions
                .Where(s => s.UserId == userId && s.IsActive)
                .ToListAsync();

            foreach (var session in sessions)
            {
                session.IsActive = false;
                session.LoggedOutAt = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<UserSession?> CreateSessionAsync(int userId, string ipAddress, string userAgent)
        {
            var sessionToken = GenerateSessionToken();
            
            var session = new UserSession
            {
                UserId = userId,
                SessionToken = sessionToken,
                IpAddress = ipAddress,
                UserAgent = userAgent,
                ExpiresAt = DateTime.UtcNow.AddDays(30), // 30 days
                CreatedAt = DateTime.UtcNow,
                LastActivityAt = DateTime.UtcNow,
                IsActive = true
            };

            _context.UserSessions.Add(session);
            await _context.SaveChangesAsync();

            return session;
        }

        public async Task<bool> ValidateSessionAsync(string sessionToken)
        {
            var session = await _context.UserSessions
                .FirstOrDefaultAsync(s => s.SessionToken == sessionToken && s.IsActive && s.ExpiresAt > DateTime.UtcNow);

            if (session != null)
            {
                session.LastActivityAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();
                return true;
            }

            return false;
        }

        public async Task<User?> GetUserBySessionAsync(string sessionToken)
        {
            var session = await _context.UserSessions
                .Include(s => s.User)
                .FirstOrDefaultAsync(s => s.SessionToken == sessionToken && s.IsActive && s.ExpiresAt > DateTime.UtcNow);

            return session?.User;
        }

        public string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var hashedBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(hashedBytes);
        }

        public string GenerateSessionToken()
        {
            var bytes = new byte[32];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            return Convert.ToBase64String(bytes);
        }

        public bool VerifyPassword(string password, string hash)
        {
            var computedHash = HashPassword(password);
            return computedHash == hash;
        }
    }
}