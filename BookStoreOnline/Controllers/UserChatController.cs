using System;
using System.Linq;
using System.Web.Mvc;
using BookStoreOnline.Models;

namespace BookStoreOnline.Controllers
{
    public class UserChatController : Controller
    {
        private NhaSachEntities3 db = new NhaSachEntities3();

        // Check and create table, and perform 7-day cleanup
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
                // Log exception if necessary or fail gracefully
                System.Diagnostics.Debug.WriteLine("Chat DB Init Error: " + ex.Message);
            }
        }

        // GET: UserChat
        public ActionResult Index()
        {
            var user = Session["TaiKhoan"] as KHACHHANG;
            if (user == null)
            {
                return RedirectToAction("Login", "User");
            }

            InitializeChatDatabase();

            ViewBag.User = user;
            return View();
        }

        [HttpGet]
        public JsonResult GetHistory()
        {
            var user = Session["TaiKhoan"] as KHACHHANG;
            if (user == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập." }, JsonRequestBehavior.AllowGet);
            }

            InitializeChatDatabase();

            try
            {
                var history = db.Database.SqlQuery<AdminChatMessage>(
                    "SELECT * FROM [dbo].[AdminChatMessages] WHERE [MaKH] = @p0 ORDER BY [CreatedAt] ASC", 
                    user.MaKH
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

        [HttpPost]
        public JsonResult Send(string message)
        {
            var user = Session["TaiKhoan"] as KHACHHANG;
            if (user == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập." });
            }

            if (string.IsNullOrWhiteSpace(message))
            {
                return Json(new { success = false, message = "Tin nhắn không được để trống." });
            }

            InitializeChatDatabase();

            try
            {
                var result = db.Database.SqlQuery<InsertResult>(
                    @"INSERT INTO [dbo].[AdminChatMessages] ([MaKH], [SenderRole], [SenderId], [SenderName], [Message], [CreatedAt], [IsRead])
                      OUTPUT INSERTED.[Id], INSERTED.[CreatedAt]
                      VALUES (@p0, @p1, @p2, @p3, @p4, @p5, @p6)",
                    user.MaKH, "User", user.MaKH, user.Ten, message.Trim(), DateTime.Now, false
                ).FirstOrDefault();

                var formattedMessage = new {
                    Id = result.Id,
                    SenderRole = "User",
                    SenderName = user.Ten,
                    Message = message.Trim(),
                    CreatedAtFormatted = result.CreatedAt.ToString("dd/MM/yyyy HH:mm:ss"),
                    IsRead = false
                };

                return Json(new { success = true, msgObj = formattedMessage });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = ex.Message });
            }
        }

        [HttpGet]
        public JsonResult GetNewMessages(int lastId)
        {
            var user = Session["TaiKhoan"] as KHACHHANG;
            if (user == null)
            {
                return Json(new { success = false, message = "Vui lòng đăng nhập." }, JsonRequestBehavior.AllowGet);
            }

            try
            {
                var newMessages = db.Database.SqlQuery<AdminChatMessage>(
                    "SELECT * FROM [dbo].[AdminChatMessages] WHERE [MaKH] = @p0 AND [Id] > @p1 ORDER BY [CreatedAt] ASC",
                    user.MaKH, lastId
                ).ToList();

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
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }

    public class InsertResult
    {
        public int Id { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
