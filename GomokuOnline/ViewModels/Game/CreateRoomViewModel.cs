using System.ComponentModel.DataAnnotations;

namespace GomokuOnline.ViewModels.Game
{
    public class CreateRoomViewModel
    {
        [Required(ErrorMessage = "Tên phòng là bắt buộc")]
        [StringLength(100, MinimumLength = 3, ErrorMessage = "Tên phòng phải từ 3-100 ký tự")]
        public string Name { get; set; } = string.Empty;

        [StringLength(500, ErrorMessage = "Mô tả không được quá 500 ký tự")]
        public string? Description { get; set; }

        [Range(2, 4, ErrorMessage = "Số người chơi tối đa từ 2-4")]
        public int MaxPlayers { get; set; } = 2;

        [Range(10, 20, ErrorMessage = "Kích thước bàn cờ từ 10x10 đến 20x20")]
        public int BoardSize { get; set; } = 15;

        [Range(3, 10, ErrorMessage = "Điều kiện thắng từ 3-10 quân liên tiếp")]
        public int WinCondition { get; set; } = 5;

        public bool IsPrivate { get; set; } = false;

        [StringLength(50, ErrorMessage = "Mật khẩu không được quá 50 ký tự")]
        public string? Password { get; set; }

        [Range(1, 60, ErrorMessage = "Thời gian giới hạn từ 1-60 phút")]
        public int TimeLimitMinutes { get; set; } = 30;

        // For GameController compatibility
        public int GameRoomId { get; set; }
    }
}