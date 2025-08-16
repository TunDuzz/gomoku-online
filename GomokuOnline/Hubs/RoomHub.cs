using Microsoft.AspNetCore.SignalR;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using GomokuOnline.Models.Entities;
using GomokuOnline.Services.Interfaces;
using GomokuOnline.Repositories.Interfaces;

namespace GomokuOnline.Hubs
{
    [Authorize]
    public class RoomHub : Hub
    {
        private readonly IGameRepository _gameRepository;
        private readonly IUserRepository _userRepository;
        private readonly ILogger<RoomHub> _logger;

        public RoomHub(
            IGameRepository gameRepository,
            IUserRepository userRepository,
            ILogger<RoomHub> logger)
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
                _logger.LogInformation($"User {userId} connected to RoomHub");
            }
            await base.OnConnectedAsync();
        }

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var userId = GetCurrentUserId();
            if (userId.HasValue)
            {
                await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"user_{userId}");
                _logger.LogInformation($"User {userId} disconnected from RoomHub");
            }
            await base.OnDisconnectedAsync(exception);
        }

        // Join room
        public async Task JoinRoom(int roomId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue)
            {
                await Clients.Caller.SendAsync("Error", "Unauthorized");
                return;
            }

            try
            {
                // Add user to room group
                await Groups.AddToGroupAsync(Context.ConnectionId, $"room_{roomId}");
                
                // Notify other users in the room
                await Clients.Group($"room_{roomId}").SendAsync("UserJoinedRoom", userId, roomId);
                
                _logger.LogInformation($"User {userId} joined room {roomId}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error joining room {RoomId}", roomId);
                await Clients.Caller.SendAsync("Error", "Failed to join room");
            }
        }

        // Leave room
        public async Task LeaveRoom(int roomId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return;

            await Groups.RemoveFromGroupAsync(Context.ConnectionId, $"room_{roomId}");
            await Clients.Group($"room_{roomId}").SendAsync("UserLeftRoom", userId, roomId);
            
            _logger.LogInformation($"User {userId} left room {roomId}");
        }

        // Send chat message to room
        public async Task SendRoomMessage(int roomId, string message)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return;

            if (string.IsNullOrWhiteSpace(message) || message.Length > 500)
            {
                await Clients.Caller.SendAsync("Error", "Invalid message");
                return;
            }

            await Clients.Group($"room_{roomId}").SendAsync("RoomMessageReceived", new
            {
                roomId,
                userId,
                message = message.Trim(),
                timestamp = DateTime.UtcNow
            });
        }

        // Player ready status
        public async Task SetReadyStatus(int roomId, bool isReady)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return;

            await Clients.Group($"room_{roomId}").SendAsync("PlayerReadyStatusChanged", new
            {
                roomId,
                userId,
                isReady,
                timestamp = DateTime.UtcNow
            });
        }

        // Start game
        public async Task StartGame(int roomId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return;

            try
            {
                // Get the current game for this room
                var currentGame = await _gameRepository.GetActiveGamesAsync();
                var game = currentGame.FirstOrDefault(g => g.GameRoomId == roomId);
                
                // Notify all players in the room
                await Clients.Group($"room_{roomId}").SendAsync("GameStarted", new
                {
                    roomId,
                    gameId = game?.Id ?? 0,
                    startedBy = userId,
                    timestamp = DateTime.UtcNow
                });
                
                _logger.LogInformation($"Game started in room {roomId} by user {userId}, gameId: {game?.Id}");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error starting game in room {RoomId}", roomId);
                await Clients.Caller.SendAsync("Error", "Failed to start game");
            }
        }

        // Game ended
        public async Task GameEnded(int roomId, int gameId, string result)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return;

            await Clients.Group($"room_{roomId}").SendAsync("GameEnded", new
            {
                roomId,
                gameId,
                result,
                endedBy = userId,
                timestamp = DateTime.UtcNow
            });
        }

        // Room status changed
        public async Task RoomStatusChanged(int roomId, string status)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return;

            await Clients.Group($"room_{roomId}").SendAsync("RoomStatusChanged", new
            {
                roomId,
                status,
                changedBy = userId,
                timestamp = DateTime.UtcNow
            });
        }

        // Player joined/left
        public async Task PlayerJoined(int roomId, int playerId, string playerName)
        {
            await Clients.Group($"room_{roomId}").SendAsync("PlayerJoined", new
            {
                roomId,
                playerId,
                playerName,
                timestamp = DateTime.UtcNow
            });
        }

        public async Task PlayerLeft(int roomId, int playerId, string playerName)
        {
            await Clients.Group($"room_{roomId}").SendAsync("PlayerLeft", new
            {
                roomId,
                playerId,
                playerName,
                timestamp = DateTime.UtcNow
            });
        }

        // Typing indicator
        public async Task StartTyping(int roomId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return;

            await Clients.Group($"room_{roomId}").SendAsync("UserTyping", new
            {
                roomId,
                userId,
                isTyping = true
            });
        }

        public async Task StopTyping(int roomId)
        {
            var userId = GetCurrentUserId();
            if (!userId.HasValue) return;

            await Clients.Group($"room_{roomId}").SendAsync("UserTyping", new
            {
                roomId,
                userId,
                isTyping = false
            });
        }

        // Ping to keep connection alive
        public async Task Ping()
        {
            await Clients.Caller.SendAsync("Pong", DateTime.UtcNow);
        }

        private int? GetCurrentUserId()
        {
            var userIdClaim = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : null;
        }
    }
}
