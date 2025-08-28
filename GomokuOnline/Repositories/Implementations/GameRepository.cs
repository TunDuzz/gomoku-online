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
            // Kiểm tra phòng có tồn tại không
            var room = await _context.GameRooms
                .Include(r => r.Participants)
                .FirstOrDefaultAsync(r => r.Id == gameRoomId);

            if (room == null)
            {
                throw new InvalidOperationException("Phòng không tồn tại");
            }

            // Kiểm tra trạng thái phòng
            if (room.Status != RoomStatus.Waiting)
            {
                throw new InvalidOperationException("Phòng không ở trạng thái chờ");
            }

            // Lấy danh sách người chơi trong phòng (không rời phòng)
            var participants = room.Participants
                .Where(p => p.Type == ParticipantType.Player && p.LeftAt == null)
                .OrderBy(p => p.PlayerOrder)
                .ToList();

            if (participants.Count < 2)
            {
                throw new InvalidOperationException("Cần ít nhất 2 người chơi để bắt đầu");
            }

            // Kiểm tra tham số
            if (boardSize < 10 || boardSize > 20)
            {
                throw new ArgumentException("Kích thước bàn cờ phải từ 10 đến 20");
            }

            if (winCondition < 3 || winCondition > 10 || winCondition > boardSize)
            {
                throw new ArgumentException("Điều kiện thắng không hợp lệ");
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
                CurrentTurnUserId = firstPlayer.UserId,
                TimeLimitSeconds = room.TimeLimitMinutes * 60 // Chuyển phút thành giây
            };

            // Cập nhật trạng thái phòng
            room.Status = RoomStatus.Playing;
            room.StartedAt = DateTime.UtcNow;

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

            // Cập nhật trạng thái phòng
            var room = await _context.GameRooms.FindAsync(game.GameRoomId);
            if (room != null)
            {
                room.Status = RoomStatus.Finished;
                room.EndedAt = DateTime.UtcNow;
            }

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

                // Cập nhật trạng thái phòng
                var room = await _context.GameRooms.FindAsync(game.GameRoomId);
                if (room != null)
                {
                    room.Status = RoomStatus.Finished;
                    room.EndedAt = DateTime.UtcNow;
                }
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

        public async Task<List<GameRoom>> GetWaitingRoomsAsync()
        {
            return await _context.GameRooms
                .Include(r => r.Participants.Where(p => p.LeftAt == null))
                .Include(r => r.CreatedByUser)
                .Where(r => r.Status == RoomStatus.Waiting && r.Participants.Count(p => p.Type == ParticipantType.Player && p.LeftAt == null) < 2)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<bool> IsValidMoveAsync(int gameId, int row, int column)
        {
            var game = await GetByIdAsync(gameId);
            if (game == null) return false;

            // Kiểm tra tọa độ hợp lệ
            if (row < 0 || row >= game.BoardSize || column < 0 || column >= game.BoardSize)
            {
                return false;
            }

            // Kiểm tra ô đã có quân cờ chưa
            var existingMove = await _context.Moves
                .FirstOrDefaultAsync(m => m.GameId == gameId && m.Row == row && m.Column == column);

            return existingMove == null;
        }

        public async Task<Move?> GetLastMoveAsync(int gameId)
        {
            return await _context.Moves
                .Where(m => m.GameId == gameId)
                .OrderByDescending(m => m.MoveNumber)
                .FirstOrDefaultAsync();
        }
    }
}