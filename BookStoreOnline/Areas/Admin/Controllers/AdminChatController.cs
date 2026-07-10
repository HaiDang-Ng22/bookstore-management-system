using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using BookStoreOnline.Core;
using BookStoreOnline.Models;
using static BookStoreOnline.Areas.Admin.Constants.Constants;

namespace BookStoreOnline.Areas.Admin.Controllers
{
    [AdminAuthorize(AdminRole.Admin)]
    public class AdminChatController : Controller
    {
        private NhaSachEntities3 db = new NhaSachEntities3();

        private void InitializeChatDatabase()
        {
            try
            {
                string createTableSql = @"
                IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[AdminChatMessages]') AND type in (N'U'))
                BEGIN
                    CREATE TABLE [dbo].[AdminChatMessages] (
                        [Id] INT IDENTITY(1,1) PRIMARY KEY,
                        [MaKH] INT NOT NULL,
                        [SenderRole] NVARCHAR(20) NOT NULL,
                        [SenderId] INT NOT NULL,
                        [SenderName] NVARCHAR(100) NULL,
                        [Message] NVARCHAR(MAX) NOT NULL,
                        [CreatedAt] DATETIME NOT NULL DEFAULT GETDATE(),
                        [IsRead] BIT NOT NULL DEFAULT 0
                    );
                END";
                db.Database.ExecuteSqlCommand(createTableSql);

                // Delete messages older than 7 days
                string cleanupSql = "DELETE FROM [dbo].[AdminChatMessages] WHERE [CreatedAt] < DATEADD(day, -7, GETDATE())";
                db.Database.ExecuteSqlCommand(cleanupSql);
            }
            catch (Exception ex)
            {
                System.Diagnostics.Debug.WriteLine("Chat DB Init Error: " + ex.Message);
            }
        }

        // GET: Admin/AdminChat
        public ActionResult Index()
        {
            InitializeChatDatabase();
            return View();
        }

        // AJAX: Get list of customers who have sent messages (active sessions within 7 days)
        [HttpGet]
        public JsonResult GetActiveUsers()
        {
            InitializeChatDatabase();

            try
            {
                // Get distinct customers who have messages, joined with KHACHHANG table for profile info
                var activeUsers = db.Database.SqlQuery<ActiveChatUser>(@"
                    SELECT 
                        k.[MaKH],
                        k.[Ten],
                        k.[Email],
                        k.[SoDienThoai],
                        MAX(m.[CreatedAt]) AS LastMessageTime,
                        (SELECT TOP 1 msg.[Message] FROM [dbo].[AdminChatMessages] msg WHERE msg.[MaKH] = k.[MaKH] ORDER BY msg.[CreatedAt] DESC) AS LastMessage,
                        (SELECT COUNT(*) FROM [dbo].[AdminChatMessages] msg WHERE msg.[MaKH] = k.[MaKH] AND msg.[SenderRole] = 'User' AND msg.[IsRead] = 0) AS UnreadCount
                    FROM [dbo].[AdminChatMessages] m
                    INNER JOIN [dbo].[KHACHHANG] k ON k.[MaKH] = m.[MaKH]
                    GROUP BY k.[MaKH], k.[Ten], k.[Email], k.[SoDienThoai]
                    ORDER BY MAX(m.[CreatedAt]) DESC
                ").ToList();

                var result = activeUsers.Select(u => new {
                    u.MaKH,
                    u.Ten,
                    u.Email,
                    u.SoDienThoai,
                    LastMessageTimeFormatted = u.LastMessageTime.ToString("dd/MM/yyyy HH:mm"),
                    LastMessage = u.LastMessage != null && u.LastMessage.Length > 50
                        ? u.LastMessage.Substring(0, 50) + "..."
                        : u.LastMessage,
                    u.UnreadCount
                });

                return Json(new { success = true, users = result }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // AJAX: Get chat history for a specific customer
        [HttpGet]
        public JsonResult GetHistory(int maKH)
        {
            try
            {
                // Mark all user messages as read
                db.Database.ExecuteSqlCommand(
                    "UPDATE [dbo].[AdminChatMessages] SET [IsRead] = 1 WHERE [MaKH] = @p0 AND [SenderRole] = 'User' AND [IsRead] = 0",
                    maKH
                );

                var history = db.Database.SqlQuery<AdminChatMessage>(
                    "SELECT * FROM [dbo].[AdminChatMessages] WHERE [MaKH] = @p0 ORDER BY [CreatedAt] ASC",
                    maKH
                ).ToList();

                var formattedMessages = history.Select(m => new {
                    m.Id,
                    m.SenderRole,
                    m.SenderName,
                    m.Message,
                    CreatedAtFormatted = m.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss"),
                    m.IsRead
                });

                return Json(new { success = true, messages = formattedMessages }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        // AJAX: Admin sends a reply to a customer
        [HttpPost]
        public JsonResult Send(int maKH, string message)
        {
            var admin = Session["TaiKhoan"] as NHANVIEN;
            if (admin == null)
            {
                return Json(new { success = false, message = "Phiên đăng nhập hết hạn." });
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return Json(new { success = false, message = "Tin nhắn không được để trống." });
            }

            try
            {
                db.Database.ExecuteSqlCommand(
                    "INSERT INTO [dbo].[AdminChatMessages] ([MaKH], [SenderRole], [SenderId], [SenderName], [Message], [CreatedAt], [IsRead]) VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6)",
                    maKH, "Admin", admin.MaNV, admin.Ten, message.Trim(), DateTime.Now, false
                );

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        // AJAX: Polling for new messages in a specific conversation
        [HttpGet]
        public JsonResult GetNewMessages(int maKH, int lastId)
        {
            try
            {
                var newMessages = db.Database.SqlQuery<AdminChatMessage>(
                    "SELECT * FROM [dbo].[AdminChatMessages] WHERE [MaKH] = @p0 AND [Id] > @p1 ORDER BY [CreatedAt] ASC",
                    maKH, lastId
                ).ToList();

                // Mark as read
                if (newMessages.Any(m => m.SenderRole == "User" && !m.IsRead))
                {
                    db.Database.ExecuteSqlCommand(
                        "UPDATE [dbo].[AdminChatMessages] SET [IsRead] = 1 WHERE [MaKH] = @p0 AND [SenderRole] = 'User' AND [IsRead] = 0",
                        maKH
                    );
                }

                var formattedMessages = newMessages.Select(m => new {
                    m.Id,
                    m.SenderRole,
                    m.SenderName,
                    m.Message,
                    CreatedAtFormatted = m.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss"),
                    m.IsRead
                });

                return Json(new { success = true, messages = formattedMessages }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) { db.Dispose(); }
            base.Dispose(disposing);
        }
    }

    // Helper class for active user list query
    public class ActiveChatUser
    {
        public int MaKH { get; set; }
        public string Ten { get; set; }
        public string Email { get; set; }
        public string SoDienThoai { get; set; }
        public DateTime LastMessageTime { get; set; }
        public string LastMessage { get; set; }
        public int UnreadCount { get; set; }
    }
}
