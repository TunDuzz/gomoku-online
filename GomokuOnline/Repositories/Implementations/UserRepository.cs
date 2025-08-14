using Microsoft.EntityFrameworkCore;
using GomokuOnline.Data;
using GomokuOnline.Models.Entities;
using GomokuOnline.Repositories.Interfaces;

namespace GomokuOnline.Repositories.Implementations
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(GomokuDbContext context) : base(context)
        {
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Username == username && u.IsActive);
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Email == email && u.IsActive);
        }

        public async Task<User?> GetByUsernameOrEmailAsync(string usernameOrEmail)
        {
            return await _dbSet.FirstOrDefaultAsync(u =>
                (u.Username == usernameOrEmail || u.Email == usernameOrEmail) && u.IsActive);
        }

        public async Task<bool> IsUsernameExistsAsync(string username)
        {
            return await _dbSet.AnyAsync(u => u.Username == username);
        }

        public async Task<bool> IsEmailExistsAsync(string email)
        {
            return await _dbSet.AnyAsync(u => u.Email == email);
        }

        public async Task<List<User>> GetTopPlayersByRatingAsync(int count = 10)
        {
            return await _dbSet
                .Include(u => u.UserStats)
                .Where(u => u.IsActive)
                .OrderByDescending(u => u.UserStats.Rating)
                .Take(count)
                .ToListAsync();
        }

        public async Task UpdateStatisticsAsync(int userId, bool isWin, bool isDraw = false)
        {
            var userStats = await _context.UserStats.FirstOrDefaultAsync(us => us.UserId == userId);
            if (userStats == null) return;

            userStats.TotalGames++;
            
            if (isDraw)
            {
                userStats.TotalDraws++;
            }
            else if (isWin)
            {
                userStats.TotalWins++;
                userStats.Rating += 25; // Simple rating system
                if (userStats.Rating > userStats.HighestRating)
                {
                    userStats.HighestRating = userStats.Rating;
                }
            }
            else
            {
                userStats.TotalLosses++;
                userStats.Rating = Math.Max(0, userStats.Rating - 25);
            }

            userStats.LastGameAt = DateTime.UtcNow;
            userStats.UpdatedAt = DateTime.UtcNow;
            
            _context.UserStats.Update(userStats);
            await _context.SaveChangesAsync();
        }

        public async Task UpdateLastLoginAsync(int userId)
        {
            var user = await GetByIdAsync(userId);
            if (user != null)
            {
                user.LastLoginAt = DateTime.UtcNow;
                await UpdateAsync(user);
            }
        }
    }
}
