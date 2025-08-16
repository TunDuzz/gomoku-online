using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using GomokuOnline.Repositories.Interfaces;
using GomokuOnline.ViewModels.Game;
using GomokuOnline.Models.Entities;

namespace GomokuOnline.Controllers
{
    [Authorize]
    public class GameController : Controller
    {
        private readonly IGameRepository _gameRepository;
        private readonly IUserRepository _userRepository;

        public GameController(IGameRepository gameRepository, IUserRepository userRepository)
        {
            _gameRepository = gameRepository;
            _userRepository = userRepository;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Auth");

            var activeGames = await _gameRepository.GetActiveGamesAsync();
            var userGames = await _gameRepository.GetUserGamesAsync(userId, 1, 10);

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
            var game = await _gameRepository.GetGameWithDetailsAsync(id);
            if (game == null)
            {
                return NotFound();
            }

            return View(game);
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
                // Tạo game mới
                var game = await _gameRepository.CreateGameAsync(
                    model.GameRoomId, 
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
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi tạo ván cờ");
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
                var game = await _gameRepository.GetGameWithDetailsAsync(gameId);
                if (game == null)
                {
                    return Json(new { success = false, message = "Ván cờ không tồn tại" });
                }

                if (game.CurrentTurnUserId != userId)
                {
                    return Json(new { success = false, message = "Chưa đến lượt của bạn" });
                }

                // Kiểm tra ô đã có quân cờ chưa
                var existingMove = game.Moves.FirstOrDefault(m => m.Row == row && m.Column == column);
                if (existingMove != null)
                {
                    return Json(new { success = false, message = "Ô này đã có quân cờ" });
                }

                // Tạo nước đi mới
                var currentPlayer = game.GameRoom?.Participants.FirstOrDefault(p => p.UserId == userId);
                var symbol = currentPlayer?.PlayerColor ?? "X";
                
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

                // Chuyển lượt cho người chơi tiếp theo
                var players = game.GameRoom?.Participants.Where(p => p.Type == ParticipantType.Player).OrderBy(p => p.PlayerOrder).ToList();
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

                // Lưu vào database
                await _gameRepository.UpdateAsync(game);

                return Json(new { 
                    success = true, 
                    move = move,
                    currentTurnUserId = game.CurrentTurnUserId,
                    totalMoves = game.TotalMoves
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

            var userGames = await _gameRepository.GetUserGamesAsync(userId, page, 20);
            return View(userGames);
        }

        [HttpGet]
        public async Task<IActionResult> GetGamePlayers(int gameId)
        {
            try
            {
                var game = await _gameRepository.GetGameWithDetailsAsync(gameId);
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
                var game = await _gameRepository.GetGameWithDetailsAsync(gameId);
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
                var rooms = await _gameRepository.GetAllRoomsAsync();
                
                // Apply filters
                var filteredRooms = rooms.AsQueryable();
                
                if (!string.IsNullOrEmpty(keyword))
                {
                    filteredRooms = filteredRooms.Where(r => 
                        r.Name.Contains(keyword, StringComparison.OrdinalIgnoreCase) ||
                        r.Description.Contains(keyword, StringComparison.OrdinalIgnoreCase));
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