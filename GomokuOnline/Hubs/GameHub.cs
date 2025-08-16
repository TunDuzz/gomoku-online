using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using GomokuOnline.Models.Entities;
using GomokuOnline.Services.Interfaces;
using GomokuOnline.Repositories.Interfaces;

namespace GomokuOnline.Hubs
{
    [Authorize]
    public class GameHub : Hub
    {
        private readonly IGameRepository _gameRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<GameHub> _logger;

        public GameHub(
            IGameRepository gameRepository,
            IUserRepository userRepository,
            ILogger<GameHub> logger)
        {
            _gameRepository = gameRepository;
            _userRepository = userRepository;
            _logger = logger;
        }

        public override async Task OnConnectedAsync()
        {
            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, $"user_{userId}");
                _logger.LogInformation($"User {userId} connected to GameHub");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
                _logger.LogInformation($"User {userId} disconnected from GameHub");
            }
            await base.OnDisconnectedAsync(exception);
        }

        // Join game room
        public async Task JoinGame(int gameId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                await Clients.Caller.SendAsync("Error", "Unauthorized");
                return;
            }

            try
            {
                var game = await _gameRepository.GetGameWithDetailsAsync(gameId);
                if (game == null)
                {
                    await Clients.Caller.SendAsync("Error", "Game not found");
                    return;
                }

                // Check if user is participant in this game
                // First check if user is in the game room
                var isParticipant = false;
                if (game.GameRoom != null)
                {
                    isParticipant = game.GameRoom.Participants.Any(p => p.UserId == userId);
                    _logger.LogInformation($"Game {gameId}: User {userId} participant check - GameRoom exists: {game.GameRoom != null}, Participants count: {game.GameRoom.Participants.Count}, IsParticipant: {isParticipant}");
                }
                else
                {
                    _logger.LogWarning($"Game {gameId}: GameRoom is null for game");
                }

                if (!isParticipant)
                {
                    await Clients.Caller.SendAsync("Error", $"You are not a participant in this game. UserId: {userId}, GameId: {gameId}");
                    return;
                }

                await Groups.AddToGroupAsync(Context.ConnectionId, $"game_{gameId}");
                await Clients.Group($"game_{gameId}").SendAsync("UserJoinedGame", userId, gameId);
                
                _logger.LogInformation($"User {userId} joined game {gameId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining game {GameId}", gameId);
                await Clients.Caller.SendAsync("Error", "Failed to join game");
            }
        }

        // Leave game room
        public async Task LeaveGame(int gameId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"game_{gameId}");
            await Clients.Group($"game_{gameId}").SendAsync("UserLeftGame", userId, gameId);
            
            _logger.LogInformation($"User {userId} left game {gameId}");
        }

        // Make a move
        public async Task MakeMove(int gameId, int row, int column)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                await Clients.Caller.SendAsync("Error", "Unauthorized");
                return;
            }

            try
            {
                var game = await _gameRepository.GetGameWithDetailsAsync(gameId);
                if (game == null)
                {
                    await Clients.Caller.SendAsync("Error", "Game not found");
                    return;
                }

                // Validate move
                if (game.Status != GameStatus.InProgress)
                {
                    await Clients.Caller.SendAsync("Error", "Game is not in progress");
                    return;
                }

                if (game.CurrentTurnUserId != userId)
                {
                    await Clients.Caller.SendAsync("Error", "Not your turn");
                    return;
                }

                // Check if position is valid and empty
                var existingMove = game.Moves.FirstOrDefault(m => m.Row == row && m.Column == column);
                if (existingMove != null)
                {
                    await Clients.Caller.SendAsync("Error", "Position already taken");
                    return;
                }

                // Create move
                var move = new Move
                {
                    GameId = gameId,
                    UserId = userId.Value,
                    Row = row,
                    Column = column,
                    MoveNumber = game.Moves.Count + 1,
                    Symbol = game.Moves.Count % 2 == 0 ? "X" : "O",
                    IsValid = true,
                    CreatedAt = DateTime.UtcNow
                };

                // Add move to game
                game.Moves.Add(move);
                game.TotalMoves = game.Moves.Count;
                game.LastMoveAt = DateTime.UtcNow;

                // Check for win condition
                var isWin = CheckWinCondition(game, row, column, move.Symbol);
                if (isWin)
                {
                    game.Status = GameStatus.Completed;
                    game.WinnerUserId = userId;
                    game.EndedAt = DateTime.UtcNow;
                }
                else if (game.Moves.Count >= game.BoardSize * game.BoardSize)
                {
                    // Board is full - draw
                    game.Status = GameStatus.Draw;
                    game.EndedAt = DateTime.UtcNow;
                }
                else
                {
                    // Switch turn
                    var players = game.GameRoom?.Participants.Where(p => p.Type == ParticipantType.Player).ToList();
                    if (players != null && players.Count > 1)
                    {
                        var currentPlayerIndex = players.FindIndex(p => p.UserId == game.CurrentTurnUserId);
                        var nextPlayerIndex = (currentPlayerIndex + 1) % players.Count;
                        game.CurrentTurnUserId = players[nextPlayerIndex].UserId;
                    }
                }

                await _gameRepository.UpdateAsync(game);

                // Notify all players in the game
                await Clients.Group($"game_{gameId}").SendAsync("MoveMade", new
                {
                    gameId,
                    userId,
                    row,
                    column,
                    symbol = move.Symbol,
                    moveNumber = move.MoveNumber,
                    isWin,
                    gameStatus = game.Status,
                    winnerUserId = game.WinnerUserId,
                    currentTurnUserId = game.CurrentTurnUserId
                });

                _logger.LogInformation($"Move made in game {gameId}: ({row}, {column}) by user {userId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error making move in game {GameId}", gameId);
                await Clients.Caller.SendAsync("Error", "Failed to make move");
            }
        }

        // Request draw
        public async Task RequestDraw(int gameId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return;

            await Clients.Group($"game_{gameId}").SendAsync("DrawRequested", userId, gameId);
        }

        // Accept/Reject draw
        public async Task RespondToDraw(int gameId, bool accept)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return;

            if (accept)
            {
                try
                {
                    var game = await _gameRepository.GetByIdAsync(gameId);
                    if (game != null && game.Status == GameStatus.InProgress)
                    {
                        game.Status = GameStatus.Draw;
                        game.EndedAt = DateTime.UtcNow;
                        await _gameRepository.UpdateAsync(game);

                        await Clients.Group($"game_{gameId}").SendAsync("GameEnded", new
                        {
                            gameId,
                            status = GameStatus.Draw,
                            endedAt = game.EndedAt
                        });
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Error accepting draw for game {GameId}", gameId);
                }
            }

            await Clients.Group($"game_{gameId}").SendAsync("DrawResponse", userId, accept);
        }

        // Surrender
        public async Task Surrender(int gameId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return;

            try
            {
                var game = await _gameRepository.GetGameWithDetailsAsync(gameId);
                if (game != null && game.Status == GameStatus.InProgress)
                {
                    // Find opponent
                    var players = game.GameRoom?.Participants.Where(p => p.Type == ParticipantType.Player).ToList();
                    var opponent = players?.FirstOrDefault(p => p.UserId != userId);
                    
                    if (opponent != null)
                    {
                        game.Status = GameStatus.Completed;
                        game.WinnerUserId = opponent.UserId;
                        game.EndedAt = DateTime.UtcNow;
                        await _gameRepository.UpdateAsync(game);

                        await Clients.Group($"game_{gameId}").SendAsync("GameEnded", new
                        {
                            gameId,
                            status = GameStatus.Completed,
                            winnerUserId = opponent.UserId,
                            surrenderedUserId = userId,
                            endedAt = game.EndedAt
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error surrendering game {GameId}", gameId);
            }
        }

        // Send chat message
        public async Task SendMessage(int gameId, string message)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return;

            if (string.IsNullOrWhiteSpace(message) || message.Length > 500)
            {
                await Clients.Caller.SendAsync("Error", "Invalid message");
                return;
            }

            await Clients.Group($"game_{gameId}").SendAsync("MessageReceived", new
            {
                gameId,
                userId,
                message = message.Trim(),
                timestamp = DateTime.UtcNow
            });
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }

        private bool CheckWinCondition(Game game, int row, int column, string symbol)
        {
            var board = new string[game.BoardSize, game.BoardSize];
            
            // Fill board with existing moves
            foreach (var move in game.Moves)
            {
                board[move.Row, move.Column] = move.Symbol;
            }

            // Check horizontal
            var horizontalCount = 1;
            for (int c = column - 1; c >= 0 && board[row, c] == symbol; c--) horizontalCount++;
            for (int c = column + 1; c < game.BoardSize && board[row, c] == symbol; c++) horizontalCount++;

            // Check vertical
            var verticalCount = 1;
            for (int r = row - 1; r >= 0 && board[r, column] == symbol; r--) verticalCount++;
            for (int r = row + 1; r < game.BoardSize && board[r, column] == symbol; r++) verticalCount++;

            // Check diagonal (top-left to bottom-right)
            var diagonal1Count = 1;
            for (int i = 1; row - i >= 0 && column - i >= 0 && board[row - i, column - i] == symbol; i++) diagonal1Count++;
            for (int i = 1; row + i < game.BoardSize && column + i < game.BoardSize && board[row + i, column + i] == symbol; i++) diagonal1Count++;

            // Check diagonal (top-right to bottom-left)
            var diagonal2Count = 1;
            for (int i = 1; row - i >= 0 && column + i < game.BoardSize && board[row - i, column + i] == symbol; i++) diagonal2Count++;
            for (int i = 1; row + i < game.BoardSize && column - i >= 0 && board[row + i, column - i] == symbol; i++) diagonal2Count++;

            return horizontalCount >= game.WinCondition || 
                   verticalCount >= game.WinCondition || 
                   diagonal1Count >= game.WinCondition || 
                   diagonal2Count >= game.WinCondition;
        }
    }
}
