using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using GomokuOnline.Repositories.Interfaces;
using GomokuOnline.ViewModels.Game;
using GomokuOnline.Models.Entities;
using Microsoft.EntityFrameworkCore;

namespace GomokuOnline.Controllers
{
    [Authorize]
    public class RoomController : Controller
    {
        private readonly IGameRepository _gameRepository;
        private readonly IUserRepository _userRepository;
        private readonly GomokuOnline.Data.GomokuDbContext _context;

        public RoomController(
            IGameRepository gameRepository, 
            IUserRepository userRepository,
            GomokuOnline.Data.GomokuDbContext context)
        {
            _gameRepository = gameRepository;
            _userRepository = userRepository;
            _context = context;
        }

        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var rooms = await _context.GameRooms
                .Include(r => r.CreatedByUser)
                .Include(r => r.Participants)
                    .ThenInclude(p => p.User)
                .Where(r => r.Status == RoomStatus.Waiting || r.Status == RoomStatus.Playing)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();

            var viewModel = new RoomListViewModel
            {
                Rooms = rooms
            };

            return View(viewModel);
        }

        [HttpGet]
        public IActionResult Create()
        {
            return View(new CreateRoomViewModel());
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateRoomViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var userId = GetCurrentUserId();
            if (userId == 0) return RedirectToAction("Login", "Auth");

            try
            {
                var room = new GameRoom
                {
                    Name = model.Name,
                    Description = model.Description,
                    CreatedByUserId = userId,
                    CreatedAt = DateTime.UtcNow,
                    Status = RoomStatus.Waiting,
                    MaxPlayers = model.MaxPlayers,
                    BoardSize = model.BoardSize,
                    WinCondition = model.WinCondition,
                    IsPrivate = model.IsPrivate,
                    Password = model.Password,
                    TimeLimitMinutes = model.TimeLimitMinutes
                };

                _context.GameRooms.Add(room);
                await _context.SaveChangesAsync();

                // Tự động tham gia phòng
                var participant = new GameParticipant
                {
                    UserId = userId,
                    GameRoomId = room.Id,
                    Type = ParticipantType.Player,
                    JoinedAt = DateTime.UtcNow,
                    IsReady = true,
                    PlayerOrder = 1,
                    PlayerColor = "X"
                };

                _context.GameParticipants.Add(participant);
                await _context.SaveChangesAsync();

                return RedirectToAction(nameof(Details), new { id = room.Id });
            }
            catch (Exception ex)
            {
                ModelState.AddModelError(string.Empty, "Đã xảy ra lỗi khi tạo phòng");
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            var room = await _context.GameRooms
                .Include(r => r.CreatedByUser)
                .Include(r => r.Participants)
                    .ThenInclude(p => p.User)
                .Include(r => r.Games)
                .Include(r => r.ChatMessages.OrderByDescending(cm => cm.CreatedAt).Take(50))
                    .ThenInclude(cm => cm.User)
                .FirstOrDefaultAsync(r => r.Id == id);

            if (room == null)
            {
                return NotFound();
            }

            return View(room);
        }

        public class JoinRoomRequest
        {
            public int RoomId { get; set; }
            public string? Password { get; set; }
        }

        public class RoomIdRequest
        {
            public int RoomId { get; set; }
        }

        public class SetReadyRequest
        {
            public int RoomId { get; set; }
            public bool IsReady { get; set; }
        }

        [HttpPost]
        public async Task<IActionResult> Join([FromBody] JoinRoomRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Json(new { success = false, message = "Chưa đăng nhập" });

            try
            {
                var room = await _context.GameRooms.FindAsync(request.RoomId);
                if (room == null)
                {
                    return Json(new { success = false, message = "Phòng không tồn tại" });
                }

                if (room.IsPrivate && room.Password != request.Password)
                {
                    return Json(new { success = false, message = "Mật khẩu phòng không đúng" });
                }

                var existingParticipant = await _context.GameParticipants
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.GameRoomId == request.RoomId);

                if (existingParticipant != null)
                {
                    return Json(new { success = false, message = "Bạn đã tham gia phòng này" });
                }

                var participantCount = await _context.GameParticipants
                    .CountAsync(p => p.GameRoomId == request.RoomId && p.Type == ParticipantType.Player);

                if (participantCount >= room.MaxPlayers)
                {
                    return Json(new { success = false, message = "Phòng đã đầy" });
                }

                var participant = new GameParticipant
                {
                    UserId = userId,
                    GameRoomId = request.RoomId,
                    Type = ParticipantType.Player,
                    JoinedAt = DateTime.UtcNow,
                    IsReady = false,
                    PlayerOrder = participantCount + 1,
                    PlayerColor = participantCount == 0 ? "X" : "O"
                };

                _context.GameParticipants.Add(participant);
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Tham gia phòng thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> Leave([FromBody] RoomIdRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Json(new { success = false, message = "Chưa đăng nhập" });

            try
            {
                var participant = await _context.GameParticipants
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.GameRoomId == request.RoomId);

                if (participant == null)
                {
                    return Json(new { success = false, message = "Bạn chưa tham gia phòng này" });
                }

                participant.LeftAt = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                return Json(new { success = true, message = "Rời phòng thành công" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> StartGame([FromBody] RoomIdRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Json(new { success = false, message = "Chưa đăng nhập" });

            try
            {
                var room = await _context.GameRooms
                    .Include(r => r.Participants)
                        .ThenInclude(p => p.User)
                    .FirstOrDefaultAsync(r => r.Id == request.RoomId);

                if (room == null)
                {
                    return Json(new { success = false, message = "Phòng không tồn tại" });
                }

                if (room.CreatedByUserId != userId)
                {
                    return Json(new { success = false, message = "Chỉ chủ phòng mới có thể bắt đầu game" });
                }

                var allPlayers = room.Participants
                    .Where(p => p.Type == ParticipantType.Player)
                    .ToList();

                if (allPlayers.Count < 2)
                {
                    return Json(new { success = false, message = "Cần ít nhất 2 người chơi để bắt đầu" });
                }

                var readyPlayers = allPlayers.Where(p => p.IsReady).ToList();
                if (readyPlayers.Count < allPlayers.Count)
                {
                    var notReadyPlayers = allPlayers.Where(p => !p.IsReady).Select(p => p.User.Username).ToList();
                    return Json(new { success = false, message = $"Các người chơi sau chưa sẵn sàng: {string.Join(", ", notReadyPlayers)}" });
                }

                // Tạo game mới
                var game = await _gameRepository.CreateGameAsync(request.RoomId, room.BoardSize, room.WinCondition);
                if (game != null)
                {
                    room.Status = RoomStatus.Playing;
                    room.StartedAt = DateTime.UtcNow;
                    await _context.SaveChangesAsync();

                    return Json(new { success = true, gameId = game.Id, redirectUrl = $"/Game/Room/{game.Id}" });
                }

                return Json(new { success = false, message = "Không thể tạo game" });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi" });
            }
        }

        [HttpPost]
        public async Task<IActionResult> SetReady([FromBody] SetReadyRequest request)
        {
            var userId = GetCurrentUserId();
            if (userId == 0) return Json(new { success = false, message = "Chưa đăng nhập" });

            try
            {
                var participant = await _context.GameParticipants
                    .FirstOrDefaultAsync(p => p.UserId == userId && p.GameRoomId == request.RoomId);

                if (participant == null)
                {
                    return Json(new { success = false, message = "Bạn chưa ở trong phòng này" });
                }

                participant.IsReady = request.IsReady;
                await _context.SaveChangesAsync();

                return Json(new { success = true, isReady = participant.IsReady });
            }
            catch (Exception)
            {
                return Json(new { success = false, message = "Đã xảy ra lỗi" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetParticipants(int roomId)
        {
            try
            {
                var participants = await _context.GameParticipants
                    .Include(p => p.User)
                    .Include(p => p.GameRoom)
                    .Where(p => p.GameRoomId == roomId)
                    .OrderBy(p => p.PlayerOrder)
                    .Select(p => new
                    {
                        userId = p.UserId,
                        username = p.User.Username,
                        isReady = p.IsReady,
                        isOwner = p.GameRoom.CreatedByUserId == p.UserId,
                        playerOrder = p.PlayerOrder,
                        playerColor = p.PlayerColor
                    })
                    .ToListAsync();

                return Json(new { success = true, participants });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi khi lấy danh sách người chơi" });
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