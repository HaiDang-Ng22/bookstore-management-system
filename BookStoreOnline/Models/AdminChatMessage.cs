using System;

namespace BookStoreOnline.Models
{
    public class AdminChatMessage
    {
        public int Id { get; set; }
        public int MaKH { get; set; }
        public string SenderRole { get; set; } // "User" or "Admin"
        public int SenderId { get; set; }      // MaKH for user, MaNV for admin
        public string SenderName { get; set; }
        public string Message { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsRead { get; set; }
    }
}
