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
                var game = await _gameRepository.GetByIdAsync(gameId);
                if (game == null)
                {
                    return Json(new { success = false, message = "Ván cờ không tồn tại" });
                }

                if (game.CurrentTurnUserId != userId)
                {
                    return Json(new { success = false, message = "Chưa đến lượt của bạn" });
                }

                // Tạo nước đi mới
                var move = new Move
                {
                    GameId = gameId,
                    UserId = userId,
                    Row = row,
                    Column = column,
                    Symbol = game.TotalMoves % 2 == 0 ? "X" : "O",
                    MoveNumber = game.TotalMoves + 1,
                    CreatedAt = DateTime.UtcNow,
                    IsValid = true
                };

                // Cập nhật game
                game.TotalMoves++;
                game.LastMoveAt = DateTime.UtcNow;

                // TODO: Kiểm tra thắng thua và cập nhật trạng thái

                return Json(new { success = true, move = move });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi" });
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