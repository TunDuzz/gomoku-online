using GomokuOnline.Models.Entities;
using GomokuOnline.Repositories.Interfaces;
using GomokuOnline.Services.Interfaces;
using Microsoft.EntityFrameworkCore;

namespace GomokuOnline.Services.Implementations
{
    public class GameService : IGameService
    {
        private readonly IGameRepository _gameRepository;
        private readonly IUserRepository _userRepository;

        public GameService(IGameRepository gameRepository, IUserRepository userRepository)
        {
            _gameRepository = gameRepository;
            _userRepository = userRepository;
        }

        public async Task<Game?> CreateGameAsync(int gameRoomId, int boardSize, int winCondition)
        {
            try
            {
                // Gọi repository để tạo game
                var game = await _gameRepository.CreateGameAsync(gameRoomId, boardSize, winCondition);
                
                // Trả về game đã tạo với đầy đủ thông tin
                if (game != null)
                {
                    return await _gameRepository.GetGameWithDetailsAsync(game.Id);
                }
                
                return null;
            }
            catch (Exception ex)
            {
                // Log error nếu cần
                throw new InvalidOperationException($"Không thể tạo game: {ex.Message}", ex);
            }
        }

        public async Task<bool> MakeMoveAsync(int gameId, int userId, int row, int column)
        {
            var game = await _gameRepository.GetGameWithDetailsAsync(gameId);
            if (game == null) return false;

            // Kiểm tra trạng thái game
            if (game.Status != GameStatus.InProgress)
            {
                return false;
            }

            if (game.CurrentTurnUserId != userId)
            {
                return false;
            }

            // Kiểm tra người chơi có trong phòng không
            var player = game.GameRoom?.Participants.FirstOrDefault(p => p.UserId == userId && p.Type == ParticipantType.Player);
            if (player == null)
            {
                return false;
            }

            // Kiểm tra tọa độ hợp lệ
            if (row < 0 || row >= game.BoardSize || column < 0 || column >= game.BoardSize)
            {
                return false;
            }

            // Kiểm tra ô đã có quân cờ chưa
            var existingMove = game.Moves.FirstOrDefault(m => m.Row == row && m.Column == column);
            if (existingMove != null)
            {
                return false;
            }

            // Tạo nước đi mới
            var symbol = player.PlayerColor ?? "X";
            
            var move = new Move
            {
                GameId = gameId,
                UserId = userId,
                Row = row,
                Column = column,
                Symbol = symbol,
                MoveNumber = game.TotalMoves + 1,
                CreatedAt = DateTime.UtcNow,
                IsValid = true
            };

            // Thêm move vào game
            game.Moves.Add(move);
            game.TotalMoves = game.Moves.Count;
            game.LastMoveAt = DateTime.UtcNow;

            // Kiểm tra thắng thua
            var isWin = await CheckWinConditionAsync(gameId, row, column, symbol);
            if (isWin)
            {
                game.WinnerUserId = userId;
                game.Status = GameStatus.Completed;
                game.EndedAt = DateTime.UtcNow;
            }
            else if (game.TotalMoves >= game.BoardSize * game.BoardSize)
            {
                // Hòa khi bàn cờ đầy
                game.Status = GameStatus.Draw;
                game.EndedAt = DateTime.UtcNow;
            }
            else
            {
                // Chuyển lượt cho người chơi tiếp theo
                var players = game.GameRoom?.Participants
                    .Where(p => p.Type == ParticipantType.Player)
                    .OrderBy(p => p.PlayerOrder)
                    .ToList();
                
                if (players != null && players.Count > 1)
                {
                    var currentPlayerIndex = players.FindIndex(p => p.UserId == game.CurrentTurnUserId);
                    if (currentPlayerIndex >= 0)
                    {
                        var nextPlayerIndex = (currentPlayerIndex + 1) % players.Count;
                        game.CurrentTurnUserId = players[nextPlayerIndex].UserId;
                    }
                    else
                    {
                        // Fallback: chuyển cho người chơi đầu tiên
                        game.CurrentTurnUserId = players.First().UserId;
                    }
                }
            }

            // Lưu vào database
            await _gameRepository.UpdateAsync(game);
            return true;
        }

        public async Task<bool> CheckWinConditionAsync(int gameId, int row, int column, string symbol)
        {
            var game = await _gameRepository.GetGameWithDetailsAsync(gameId);
            if (game == null) return false;

            var moves = game.Moves.Where(m => m.Symbol == symbol).ToList();
            var winCondition = game.WinCondition;

            // Kiểm tra hàng ngang
            if (CheckDirection(moves, row, column, 0, 1, winCondition)) return true;
            
            // Kiểm tra hàng dọc
            if (CheckDirection(moves, row, column, 1, 0, winCondition)) return true;
            
            // Kiểm tra đường chéo chính
            if (CheckDirection(moves, row, column, 1, 1, winCondition)) return true;
            
            // Kiểm tra đường chéo phụ
            if (CheckDirection(moves, row, column, 1, -1, winCondition)) return true;

            return false;
        }

        private bool CheckDirection(List<Move> moves, int centerRow, int centerCol, int deltaRow, int deltaCol, int winCondition)
        {
            var count = 1; // Đếm quân cờ hiện tại

            // Đếm về một phía
            for (int i = 1; i < winCondition; i++)
            {
                var checkRow = centerRow + (deltaRow * i);
                var checkCol = centerCol + (deltaCol * i);
                
                if (moves.Any(m => m.Row == checkRow && m.Column == checkCol))
                {
                    count++;
                }
                else
                {
                    break;
                }
            }

            // Đếm về phía đối diện
            for (int i = 1; i < winCondition; i++)
            {
                var checkRow = centerRow - (deltaRow * i);
                var checkCol = centerCol - (deltaCol * i);
                
                if (moves.Any(m => m.Row == checkRow && m.Column == checkCol))
                {
                    count++;
                }
                else
                {
                    break;
                }
            }

            return count >= winCondition;
        }

        public async Task<bool> IsValidMoveAsync(int gameId, int row, int column)
        {
            return await _gameRepository.IsValidMoveAsync(gameId, row, column);
        }

        public async Task<Game?> GetGameWithDetailsAsync(int gameId)
        {
            return await _gameRepository.GetGameWithDetailsAsync(gameId);
        }

        public async Task<List<Game>> GetActiveGamesAsync()
        {
            return await _gameRepository.GetActiveGamesAsync();
        }

        public async Task<List<Game>> GetUserGamesAsync(int userId, int page = 1, int pageSize = 10)
        {
            return await _gameRepository.GetUserGamesAsync(userId, page, pageSize);
        }

        public async Task<bool> SetWinnerAsync(int gameId, int winnerUserId)
        {
            return await _gameRepository.SetWinnerAsync(gameId, winnerUserId);
        }

        public async Task<bool> UpdateGameStatusAsync(int gameId, GameStatus status)
        {
            return await _gameRepository.UpdateGameStatusAsync(gameId, status);
        }

        public async Task<Move?> GetLastMoveAsync(int gameId)
        {
            return await _gameRepository.GetLastMoveAsync(gameId);
        }

        public async Task<List<GameRoom>> GetAllRoomsAsync()
        {
            return await _gameRepository.GetAllRoomsAsync();
        }

        public async Task<List<GameRoom>> GetWaitingRoomsAsync()
        {
            return await _gameRepository.GetWaitingRoomsAsync();
        }
    }
}
