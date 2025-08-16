using Microsoft.EntityFrameworkCore;
using GomokuOnline.Data;
using GomokuOnline.Models.Entities;
using GomokuOnline.Repositories.Interfaces;

namespace GomokuOnline.Repositories.Implementations
{
    public class GameRepository : Repository<Game>, IGameRepository
    {
        public GameRepository(GomokuDbContext context) : base(context)
        {
        }

        public async Task<Game?> GetGameWithDetailsAsync(int gameId)
        {
            return await _dbSet
                .Include(g => g.GameRoom)
                    .ThenInclude(gr => gr.Participants)
                        .ThenInclude(p => p.User)
                .Include(g => g.WinnerUser)
                .Include(g => g.CurrentTurnUser)
                .Include(g => g.Moves)
                    .ThenInclude(m => m.User)
                .Include(g => g.GameStates)
                .AsSplitQuery() // Split query to avoid circular references
                .FirstOrDefaultAsync(g => g.Id == gameId);
        }

        public async Task<List<Game>> GetActiveGamesAsync()
        {
            return await _dbSet
                .Include(g => g.GameRoom)
                .Include(g => g.WinnerUser)
                .Include(g => g.CurrentTurnUser)
                .Where(g => g.Status == GameStatus.InProgress)
                .OrderBy(g => g.StartedAt)
                .ToListAsync();
        }

        public async Task<List<Game>> GetUserGamesAsync(int userId, int page = 1, int pageSize = 10)
        {
            return await _dbSet
                .Include(g => g.GameRoom)
                .Include(g => g.WinnerUser)
                .Include(g => g.CurrentTurnUser)
                .Where(g => g.GameRoom.Participants.Any(p => p.UserId == userId))
                .OrderByDescending(g => g.StartedAt)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
        }

        public async Task<Game?> CreateGameAsync(int gameRoomId, int boardSize, int winCondition)
        {
            // Lấy danh sách người chơi trong phòng
            var participants = await _context.GameParticipants
                .Where(p => p.GameRoomId == gameRoomId && p.Type == ParticipantType.Player)
                .OrderBy(p => p.PlayerOrder)
                .ToListAsync();

            if (!participants.Any())
            {
                throw new InvalidOperationException("Không có người chơi nào trong phòng");
            }

            // Người chơi đầu tiên (PlayerOrder = 1) sẽ chơi trước
            var firstPlayer = participants.FirstOrDefault(p => p.PlayerOrder == 1) ?? participants.First();

            var game = new Game
            {
                GameRoomId = gameRoomId,
                BoardSize = boardSize,
                WinCondition = winCondition,
                StartedAt = DateTime.UtcNow,
                Status = GameStatus.InProgress,
                TotalMoves = 0,
                CurrentTurnUserId = firstPlayer.UserId // Thiết lập người chơi đầu tiên
            };

            await _dbSet.AddAsync(game);
            await _context.SaveChangesAsync();
            return game;
        }

        public async Task<bool> SetWinnerAsync(int gameId, int winnerUserId)
        {
            var game = await GetByIdAsync(gameId);
            if (game == null) return false;

            game.WinnerUserId = winnerUserId;
            game.Status = GameStatus.Completed;
            game.EndedAt = DateTime.UtcNow;

            await UpdateAsync(game);
            return true;
        }

        public async Task<bool> SetCurrentTurnAsync(int gameId, int currentTurnUserId)
        {
            var game = await GetByIdAsync(gameId);
            if (game == null) return false;

            game.CurrentTurnUserId = currentTurnUserId;
            game.LastMoveAt = DateTime.UtcNow;

            await UpdateAsync(game);
            return true;
        }

        public async Task<bool> UpdateGameStatusAsync(int gameId, GameStatus status)
        {
            var game = await GetByIdAsync(gameId);
            if (game == null) return false;

            game.Status = status;
            
            if (status == GameStatus.Completed || status == GameStatus.Draw || 
                status == GameStatus.Cancelled || status == GameStatus.Timeout)
            {
                game.EndedAt = DateTime.UtcNow;
            }

            await UpdateAsync(game);
            return true;
        }

        public async Task<List<GameRoom>> GetAllRoomsAsync()
        {
            return await _context.GameRooms
                .Include(r => r.Participants)
                .Include(r => r.CreatedByUser)
                .Include(r => r.Games)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }
    }
}