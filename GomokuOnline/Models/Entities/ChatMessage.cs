using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace GomokuOnline.Models.Entities
{
    [Table("ChatMessages")]
    public class ChatMessage
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public int UserId { get; set; }

        [Required]
        public int GameRoomId { get; set; }

        [Required]
        [StringLength(1000)]
        public string Content { get; set; } = string.Empty;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public DateTime? EditedAt { get; set; }

        public bool IsEdited { get; set; } = false;

        public bool IsDeleted { get; set; } = false;

        public DateTime? DeletedAt { get; set; }

        public int? ReplyToMessageId { get; set; } // Trả lời tin nhắn nào

        [Required]
        public MessageType Type { get; set; } = MessageType.Text;

        // Navigation properties
        [ForeignKey("UserId")]
        public virtual User User { get; set; } = null!;

        [ForeignKey("GameRoomId")]
        public virtual GameRoom GameRoom { get; set; } = null!;

        [ForeignKey("ReplyToMessageId")]
        public virtual ChatMessage? ReplyToMessage { get; set; }

        public virtual ICollection<ChatMessage> Replies { get; set; } = new List<ChatMessage>();
    }

    public enum MessageType
    {
        Text = 0,
        System = 1,
        GameEvent = 2,
        Emoji = 3
    }
}