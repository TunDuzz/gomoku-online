using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using GomokuOnline.Repositories.Interfaces;
using GomokuOnline.Services.Interfaces;
using GomokuOnline.ViewModels.Game;
using GomokuOnline.Models.Entities;
using Microsoft.AspNetCore.SignalR;
using GomokuOnline.Hubs;

namespace GomokuOnline.Controllers
{
    [Authorize]
    public class GameController : Controller
    {
        private readonly IGameService _gameService;
        private readonly IUserRepository _userRepository;
        private readonly IHubContext<GameHub> _gameHub;

        public GameController(IGameService gameService, IUserRepository userRepository, IHubContext<GameHub> gameHub)
        {
            _gameService = gameService;
            _userRepository = userRepository;
            _gameHub = gameHub;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Auth");

            var activeGames = await _gameService.GetActiveGamesAsync();
            var userGames = await _gameService.GetUserGamesAsync(userId, 1, 10);

            var viewModel = new GameViewModel
            {
                ActiveGames = activeGames,
                UserGames = userGames
            };

            return View(viewModel);
        }

        [HttpGet]
        public async Task<IActionResult> Room(int id)
        {
            var game = await _gameService.GetGameWithDetailsAsync(id);
            if (game == null)
            {
                return NotFound();
            }

            return View(game);
        }

        [HttpGet]
        public async Task<IActionResult> DebugGame(int id)
        {
            try
            {
                var game = await _gameService.GetGameWithDetailsAsync(id);
                if (game == null)
                {
                    return Json(new { 
                        success = false, 
                        message = "Game không tồn tại",
                        gameId = id
                    });
                }

                return Json(new { 
                    success = true, 
                    game = new
                    {
                        id = game.Id,
                        gameRoomId = game.GameRoomId,
                        status = game.Status,
                        boardSize = game.BoardSize,
                        winCondition = game.WinCondition,
                        currentTurnUserId = game.CurrentTurnUserId,
                        totalMoves = game.TotalMoves,
                        startedAt = game.StartedAt,
                        endedAt = game.EndedAt
                    }
                });
            }
            catch (Exception ex)
            {
                return Json(new { 
                    success = false, 
                    message = "Lỗi: " + ex.Message,
                    gameId = id
                });
            }
        }

        [HttpGet]
        public IActionResult CreateRoom()
        {
            return View(new CreateRoomViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> CreateRoom(CreateRoomViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Auth");

            try
            {
                // Kiểm tra xem có GameRoomId không
                if (!model.GameRoomId.HasValue)
                {
                    ModelState.AddModelError(string.Empty, "Vui lòng chọn phòng để tạo game");
                    return View(model);
                }

                // Tạo game mới
                var game = await _gameService.CreateGameAsync(
                    model.GameRoomId.Value, 
                    model.BoardSize, 
                    model.WinCondition);

                if (game != null)
                {
                    return RedirectToAction(nameof(Room), new { id = game.Id });
                }

                ModelState.AddModelError(string.Empty, "Không thể tạo ván cờ mới");
                return View(model);
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi tạo ván cờ: " + ex.Message);
                return View(model);
            }
        }

        [HttpPost]
        public async Task<IActionResult> MakeMove(int gameId, int row, int column)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Json(new { success = false, message = "Chưa đăng nhập" });

            try
            {
                var game = await _gameService.GetGameWithDetailsAsync(gameId);
                if (game == null)
                {
                    return Json(new { success = false, message = "Ván cờ không tồn tại" });
                }

                // Kiểm tra trạng thái game
                if (game.Status != GameStatus.InProgress)
                {
                    return Json(new { success = false, message = "Ván cờ đã kết thúc" });
                }

                if (game.CurrentTurnUserId != userId)
                {
                    return Json(new { success = false, message = "Chưa đến lượt của bạn" });
                }

                // Kiểm tra người chơi có trong phòng không
                var player = game.GameRoom?.Participants.FirstOrDefault(p => p.UserId == userId && p.Type == ParticipantType.Player);
                if (player == null)
                {
                    return Json(new { success = false, message = "Bạn không phải người chơi trong ván cờ này" });
                }

                // Thực hiện nước đi
                var success = await _gameService.MakeMoveAsync(gameId, userId, row, column);
                if (!success)
                {
                    return Json(new { success = false, message = "Nước đi không hợp lệ" });
                }

                // Lấy thông tin game sau khi thực hiện nước đi
                var updatedGame = await _gameService.GetGameWithDetailsAsync(gameId);
                var lastMove = await _gameService.GetLastMoveAsync(gameId);

                // Phát SignalR event
                await _gameHub.Clients.Group($"game_{gameId}").SendAsync("MoveMade", new
                {
                    gameId = gameId,
                    userId = userId,
                    row = row,
                    column = column,
                    symbol = lastMove?.Symbol,
                    moveNumber = lastMove?.MoveNumber,
                    isWin = updatedGame?.Status == GameStatus.Completed,
                    gameStatus = updatedGame?.Status,
                    winnerUserId = updatedGame?.WinnerUserId,
                    currentTurnUserId = updatedGame?.CurrentTurnUserId
                });

                // Broadcast game ended event if game is completed
                if (updatedGame?.Status == GameStatus.Completed)
                {
                    await _gameHub.Clients.All.SendAsync("GameEnded", new { gameId });
                }

                return Json(new { 
                    success = true, 
                    move = lastMove,
                    currentTurnUserId = updatedGame?.CurrentTurnUserId,
                    totalMoves = updatedGame?.TotalMoves,
                    gameStatus = updatedGame?.Status,
                    winnerUserId = updatedGame?.WinnerUserId,
                    isWin = updatedGame?.Status == GameStatus.Completed
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi: " + ex.Message });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GameHistory(int page = 1)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Auth");

            var userGames = await _gameService.GetUserGamesAsync(userId, page, 20);
            return View(userGames);
        }

        [HttpGet]
        public async Task<IActionResult> GetGamePlayers(int gameId)
        {
            try
            {
                var game = await _gameService.GetGameWithDetailsAsync(gameId);
                if (game == null)
                {
                    return Json(new { success = false, message = "Game không tồn tại" });
                }

                object players;
                if (game.GameRoom?.Participants != null)
                {
                    players = game.GameRoom.Participants
                        .Where(p => p.Type == ParticipantType.Player)
                        .OrderBy(p => p.PlayerOrder)
                        .Select(p => new
                        {
                            userId = p.UserId,
                            username = p.User.Username,
                            playerColor = p.PlayerColor,
                            playerOrder = p.PlayerOrder,
                            isCurrentTurn = p.UserId == game.CurrentTurnUserId
                        })
                        .ToList();
                }
                else
                {
                    players = new List<object>();
                }

                return Json(new { success = true, players });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi lấy danh sách người chơi" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetGameBoard(int gameId)
        {
            try
            {
                var game = await _gameService.GetGameWithDetailsAsync(gameId);
                if (game == null)
                {
                    return PartialView("_GameBoardError", "Game không tồn tại");
                }

                return PartialView("_GameBoard", game);
            }
            catch (Exception ex)
            {
                return PartialView("_GameBoardError", "Lỗi khi tải bàn cờ");
            }
        }

        [HttpGet]
        public async Task<IActionResult> SearchRooms(string keyword = "", string status = "", string boardSize = "")
        {
            try
            {
                // Get all rooms from database
                var rooms = await _gameService.GetAllRoomsAsync();
                
                // Apply filters - only show active rooms (Waiting or Playing)
                var filteredRooms = rooms.Where(r => r.Status == RoomStatus.Waiting || r.Status == RoomStatus.Playing).AsQueryable();
                
                if (!string.IsNullOrEmpty(keyword))
                {
                    filteredRooms = filteredRooms.Where(r => 
                        r.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                        (r.Description != null && r.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase)));
                }
                
                if (!string.IsNullOrEmpty(status))
                {
                    if (Enum.TryParse<RoomStatus>(status, out var statusEnum))
                    {
                        filteredRooms = filteredRooms.Where(r => r.Status == statusEnum);
                    }
                }
                
                if (!string.IsNullOrEmpty(boardSize))
                {
                    if (int.TryParse(boardSize, out var size))
                    {
                        filteredRooms = filteredRooms.Where(r => r.BoardSize == size);
                    }
                }

                // Convert to anonymous objects for JSON response
                var result = filteredRooms
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(12) // Limit to 12 rooms
                    .ToList()
                    .Select(r => new
                    {
                        id = r.Id,
                        name = r.Name,
                        status = r.Status.ToString(),
                        playerCount = r.Participants.Count(p => p.Type == ParticipantType.Player),
                        boardSize = r.BoardSize,
                        winCondition = r.WinCondition,
                        createdBy = r.CreatedByUser != null ? r.CreatedByUser.Username : "Unknown",
                        createdAt = r.CreatedAt
                    })
                    .ToList();

                return Json(new { success = true, rooms = result });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi tìm kiếm phòng" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetGameState(int id)
        {
            try
            {
                var game = await _gameService.GetGameWithDetailsAsync(id);
                if (game == null)
                {
                    return Json(new { success = false, message = "Game không tồn tại" });
                }

                var gameState = new
                {
                    id = game.Id,
                    status = game.Status.ToString(),
                    currentTurnUserId = game.CurrentTurnUserId,
                    totalMoves = game.TotalMoves,
                    winnerUserId = game.WinnerUserId,
                    startedAt = game.StartedAt,
                    endedAt = game.EndedAt
                };

                return Json(new { success = true, game = gameState });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi lấy trạng thái game" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetLatestMoves(int id, int lastMoveCount = 0)
        {
            try
            {
                var game = await _gameService.GetGameWithDetailsAsync(id);
                if (game == null)
                {
                    return Json(new { success = false, message = "Game không tồn tại" });
                }

                // Get moves after the last move count
                var latestMoves = game.Moves
                    .Where(m => m.MoveNumber > lastMoveCount)
                    .OrderBy(m => m.MoveNumber)
                    .Select(m => new
                    {
                        row = m.Row,
                        col = m.Column,
                        symbol = m.Symbol,
                        username = m.User?.Username ?? "Unknown",
                        moveNumber = m.MoveNumber,
                        createdAt = m.CreatedAt
                    })
                    .ToList();

                return Json(new { success = true, moves = latestMoves });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi lấy nước đi mới" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetActiveGames()
        {
            try
            {
                var userId = GetCurrentUserId();
                var activeGames = await _gameService.GetActiveGamesAsync();
                var userGames = await _gameService.GetUserGamesAsync(userId, 1, 10);

                // Lấy thêm các phòng đang chờ người chơi
                var waitingRooms = await _gameService.GetWaitingRoomsAsync();

                // Convert active games to anonymous objects
                var gamesData = activeGames
                    .OrderByDescending(g => g.StartedAt)
                    .Take(6)
                    .Select(g => new
                    {
                        id = g.Id,
                        roomName = g.GameRoom?.Name ?? "Unknown Room",
                        boardSize = g.BoardSize,
                        winCondition = g.WinCondition,
                        totalMoves = g.TotalMoves,
                        playerCount = g.GameRoom?.Participants.Count(p => p.Type == ParticipantType.Player) ?? 0,
                        startedAt = g.StartedAt,
                        status = g.Status.ToString(),
                        currentTurnUserId = g.CurrentTurnUserId,
                        type = "active"
                    })
                    .ToList();

                // Convert waiting rooms to anonymous objects
                var waitingData = waitingRooms
                    .OrderByDescending(r => r.CreatedAt)
                    .Take(6 - gamesData.Count) // Fill remaining slots
                    .Select(r => new
                    {
                        id = r.Id,
                        roomName = r.Name,
                        boardSize = r.BoardSize,
                        winCondition = r.WinCondition,
                        totalMoves = 0,
                        playerCount = r.Participants.Count(p => p.Type == ParticipantType.Player),
                        startedAt = r.CreatedAt,
                        status = "Waiting",
                        currentTurnUserId = (int?)null,
                        type = "waiting"
                    })
                    .ToList();

                // Combine both lists
                var allData = gamesData.Concat(waitingData).ToList();

                return Json(new { 
                    success = true, 
                    games = allData,
                    userGamesCount = userGames.Count
                });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi lấy danh sách game" });
            }
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null && int.TryParse(userIdClaim.Value, out int userId))
            {
                return userId;
            }
            return 0;
        }
    }
} 