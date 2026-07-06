using System;
using System.Collections.Generic;
using System.Data.SqlClient;
using System.IO;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using BookStoreOnline.Areas.Shipper.Models;
using BookStoreOnline.Core;
using BookStoreOnline.Models;
using static BookStoreOnline.Areas.Admin.Constants.Constants;

namespace BookStoreOnline.Areas.Shipper.Controllers
{
    [ShipperAuthorize]
    public class OrdersShipperController : Controller
    {
        private readonly NhaSachEntities3 db = new NhaSachEntities3();
        private NHANVIEN CurrentShipper => Session["TaiKhoan"] as NHANVIEN;

        public ActionResult Index(string tab = "delivering", int? page = 1, DateTime? fromDate = null,
            DateTime? toDate = null, int? orderId = null, string customerName = null,
            string phone = null, int? paymentType = null)
        {
            var shipper = CurrentShipper;
            if (shipper == null) return RedirectToAction("Login", "User", new { area = "" });

            var query = db.DONHANGs.AsQueryable();
            switch (tab)
            {
                case "available": query = query.Where(x => x.TrangThai == (int)StatusOrder.Informed && x.MaNVXuLy == null); break;
                case "delivered": query = query.Where(x => x.MaNVXuLy == shipper.MaNV && x.TrangThai == (int)StatusOrder.Received); break;
                case "failed": query = query.Where(x => x.MaNVXuLy == shipper.MaNV && (x.TrangThai == (int)StatusOrder.DeliveryFailed || x.TrangThai == (int)StatusOrder.Rescheduled || x.TrangThai == (int)StatusOrder.Returning || x.TrangThai == (int)StatusOrder.Returned)); break;
                default: tab = "delivering"; query = query.Where(x => x.MaNVXuLy == shipper.MaNV && x.TrangThai == (int)StatusOrder.Shipping); break;
            }

            if (fromDate.HasValue) query = query.Where(x => x.NgayDat >= fromDate.Value);
            if (toDate.HasValue) { var end = toDate.Value.Date.AddDays(1); query = query.Where(x => x.NgayDat < end); }
            if (orderId.HasValue) query = query.Where(x => x.MaDonHang == orderId.Value);
            if (!string.IsNullOrWhiteSpace(customerName)) query = query.Where(x => x.KHACHHANG.Ten.Contains(customerName));
            if (!string.IsNullOrWhiteSpace(phone)) query = query.Where(x => x.KHACHHANG.SoDienThoai.Contains(phone));
            if (paymentType.HasValue) query = query.Where(x => x.PhuongThucThanhToan == paymentType.Value);

            const int pageSize = 10;
            var total = query.Count();
            var currentPage = Math.Max(1, page ?? 1);
            ViewBag.CurrentTab = tab;
            ViewBag.TotalCount = total;
            ViewBag.TotalPages = Math.Max(1, (int)Math.Ceiling(total / (double)pageSize));
            ViewBag.CurrentPage = currentPage;
            return View(query.OrderByDescending(x => x.NgayDat).Skip((currentPage - 1) * pageSize).Take(pageSize).ToList());
        }

        public ActionResult Details(int? id)
        {
            if (!id.HasValue) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var shipper = CurrentShipper;
            var order = db.DONHANGs.FirstOrDefault(x => x.MaDonHang == id.Value);
            if (order == null) return HttpNotFound();
            var canView = order.MaNVXuLy == shipper.MaNV || (order.TrangThai == (int)StatusOrder.Informed && order.MaNVXuLy == null);
            if (!canView) { TempData["ErrorMessage"] = "Bạn không có quyền xem đơn hàng này."; return RedirectToAction("Index"); }
            ViewBag.Detail = db.CHITIETDONHANGs.Where(x => x.MaDonHang == id.Value).ToList();
            try
            {
                EnsureDeliveryHistoryTable();
                ViewBag.History = LoadHistory(id.Value);
            }
            catch (SqlException)
            {
                ViewBag.History = new List<DeliveryHistoryItem>();
                ViewBag.HistoryWarning = "Chưa thể tải lịch sử giao hàng. Vui lòng chạy script UpgradeShipperDelivery.sql trên database.";
            }
            return View(order);
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult AcceptOrder(int id)
        {
            var shipper = CurrentShipper;
            var affected = db.Database.ExecuteSqlCommand(
                "UPDATE dbo.DONHANG SET MaNVXuLy=@shipper, TrangThai=@shipping WHERE MaDonHang=@id AND TrangThai=@waiting AND MaNVXuLy IS NULL",
                new SqlParameter("@shipper", shipper.MaNV), new SqlParameter("@shipping", (int)StatusOrder.Shipping),
                new SqlParameter("@id", id), new SqlParameter("@waiting", (int)StatusOrder.Informed));
            if (affected != 1) { TempData["ErrorMessage"] = "Đơn đã được shipper khác nhận hoặc không còn chờ giao."; return RedirectToAction("Index", new { tab = "available" }); }
            AddHistory(id, shipper.MaNV, (int)StatusOrder.Shipping, null, "Shipper đã nhận đơn", null, null);
            TempData["SuccessMessage"] = "Đã nhận đơn và bắt đầu giao hàng.";
            return RedirectToAction("Details", new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult DeliverySuccess(DeliverySuccessViewModel model)
        {
            var order = OwnedShippingOrder(model.OrderId);
            if (order == null) return InvalidOperationMessage();
            if (!ModelState.IsValid) { TempData["ErrorMessage"] = "Ghi chú không được vượt quá 1000 ký tự."; return RedirectToAction("Details", new { id = model.OrderId }); }
            string imagePath;
            if (!TrySaveEvidence(model.EvidenceImage, model.OrderId, out imagePath)) return RedirectToAction("Details", new { id = model.OrderId });
            order.TrangThai = (int)StatusOrder.Received;
            if (order.PhuongThucThanhToan == (int)TypePayment.COD) order.TrangThaiThanhToan = (int)StatusPayment.Paid;
            db.SaveChanges();
            AddHistory(order.MaDonHang, CurrentShipper.MaNV, (int)StatusOrder.Received, null, model.Note, imagePath, null);
            if (order.ID.HasValue) new CustomerTypeService(db).UpdateCustomerType(order.ID.Value);
            TempData["SuccessMessage"] = "Đã xác nhận giao hàng thành công.";
            return RedirectToAction("Index", new { tab = "delivered" });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult DeliveryFailed(DeliveryFailureViewModel model)
        {
            var order = OwnedShippingOrder(model.OrderId);
            if (order == null) return InvalidOperationMessage();
            if (!ModelState.IsValid || !model.Reason.HasValue || string.IsNullOrWhiteSpace(model.Note)) { TempData["ErrorMessage"] = "Phải chọn lý do, nhập ghi chú (tối đa 1000 ký tự)."; return RedirectToAction("Details", new { id = model.OrderId }); }
            if (model.Reschedule && (!model.RescheduledAt.HasValue || model.RescheduledAt <= DateTime.Now)) { TempData["ErrorMessage"] = "Thời gian giao lại phải ở tương lai."; return RedirectToAction("Details", new { id = model.OrderId }); }
            string imagePath;
            if (!TrySaveEvidence(model.EvidenceImage, model.OrderId, out imagePath)) return RedirectToAction("Details", new { id = model.OrderId });
            var status = model.Reschedule ? StatusOrder.Rescheduled : StatusOrder.DeliveryFailed;
            order.TrangThai = (int)status;
            db.SaveChanges();
            AddHistory(order.MaDonHang, CurrentShipper.MaNV, (int)status, GetReasonName(model.Reason.Value), model.Note, imagePath, model.RescheduledAt);
            TempData["SuccessMessage"] = model.Reschedule ? "Đã lưu lịch hẹn giao lại." : "Đã ghi nhận giao hàng thất bại.";
            return RedirectToAction("Index", new { tab = "failed" });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult StartRedelivery(int id)
        {
            var affected = db.Database.ExecuteSqlCommand("UPDATE dbo.DONHANG SET TrangThai=@shipping WHERE MaDonHang=@id AND MaNVXuLy=@shipper AND TrangThai IN (@failed,@rescheduled)",
                new SqlParameter("@shipping", (int)StatusOrder.Shipping), new SqlParameter("@id", id), new SqlParameter("@shipper", CurrentShipper.MaNV),
                new SqlParameter("@failed", (int)StatusOrder.DeliveryFailed), new SqlParameter("@rescheduled", (int)StatusOrder.Rescheduled));
            if (affected == 1) { AddHistory(id, CurrentShipper.MaNV, (int)StatusOrder.Shipping, null, "Bắt đầu giao lại", null, null); TempData["SuccessMessage"] = "Đơn đã chuyển sang đang giao."; }
            else TempData["ErrorMessage"] = "Không thể giao lại đơn này.";
            return RedirectToAction("Details", new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult StartReturning(int id, string note)
        {
            var affected = db.Database.ExecuteSqlCommand("UPDATE dbo.DONHANG SET TrangThai=@returning WHERE MaDonHang=@id AND MaNVXuLy=@shipper AND TrangThai IN (@failed,@rescheduled)",
                new SqlParameter("@returning", (int)StatusOrder.Returning), new SqlParameter("@id", id), new SqlParameter("@shipper", CurrentShipper.MaNV),
                new SqlParameter("@failed", (int)StatusOrder.DeliveryFailed), new SqlParameter("@rescheduled", (int)StatusOrder.Rescheduled));
            if (affected == 1) { AddHistory(id, CurrentShipper.MaNV, (int)StatusOrder.Returning, null, note, null, null); TempData["SuccessMessage"] = "Đơn đang được hoàn về kho."; }
            else TempData["ErrorMessage"] = "Không thể hoàn đơn này.";
            return RedirectToAction("Details", new { id });
        }

        [HttpPost, ValidateAntiForgeryToken]
        public ActionResult ConfirmReturned(int id, string note)
        {
            var affected = db.Database.ExecuteSqlCommand("UPDATE dbo.DONHANG SET TrangThai=@returned WHERE MaDonHang=@id AND MaNVXuLy=@shipper AND TrangThai=@returning",
                new SqlParameter("@returned", (int)StatusOrder.Returned), new SqlParameter("@id", id), new SqlParameter("@shipper", CurrentShipper.MaNV), new SqlParameter("@returning", (int)StatusOrder.Returning));
            if (affected == 1) { AddHistory(id, CurrentShipper.MaNV, (int)StatusOrder.Returned, null, note, null, null); TempData["SuccessMessage"] = "Đã xác nhận hoàn hàng về kho."; }
            else TempData["ErrorMessage"] = "Không thể xác nhận hoàn kho.";
            return RedirectToAction("Details", new { id });
        }

        private DONHANG OwnedShippingOrder(int id) => db.DONHANGs.FirstOrDefault(x => x.MaDonHang == id && x.MaNVXuLy == CurrentShipper.MaNV && x.TrangThai == (int)StatusOrder.Shipping);
        private ActionResult InvalidOperationMessage() { TempData["ErrorMessage"] = "Đơn không thuộc quyền xử lý hoặc không ở trạng thái đang giao."; return RedirectToAction("Index"); }

        private void AddHistory(int orderId, int shipperId, int status, string reason, string note, string image, DateTime? rescheduledAt)
        {
            EnsureDeliveryHistoryTable();
            db.Database.ExecuteSqlCommand("INSERT INTO dbo.LICHSUGIAOHANG(MaDonHang,MaNhanVien,TrangThai,LyDo,GhiChu,AnhBangChung,ThoiGianHenLai) VALUES(@order,@staff,@status,@reason,@note,@image,@rescheduled)",
                new SqlParameter("@order", orderId), new SqlParameter("@staff", shipperId), new SqlParameter("@status", status),
                new SqlParameter("@reason", (object)reason ?? DBNull.Value), new SqlParameter("@note", (object)note ?? DBNull.Value),
                new SqlParameter("@image", (object)image ?? DBNull.Value), new SqlParameter("@rescheduled", (object)rescheduledAt ?? DBNull.Value));
        }

        private List<DeliveryHistoryItem> LoadHistory(int orderId) => db.Database.SqlQuery<DeliveryHistoryItem>(
            "SELECT Id,MaDonHang OrderId,TrangThai Status,LyDo Reason,GhiChu Note,AnhBangChung EvidenceImage,ThoiGian CreatedAt,ThoiGianHenLai RescheduledAt FROM dbo.LICHSUGIAOHANG WHERE MaDonHang=@id ORDER BY ThoiGian DESC", new SqlParameter("@id", orderId)).ToList();

        private void EnsureDeliveryHistoryTable()
        {
            db.Database.ExecuteSqlCommand(@"
IF OBJECT_ID(N'dbo.LICHSUGIAOHANG', N'U') IS NULL
BEGIN
    CREATE TABLE dbo.LICHSUGIAOHANG
    (
        Id INT IDENTITY(1,1) NOT NULL CONSTRAINT PK_LICHSUGIAOHANG PRIMARY KEY,
        MaDonHang INT NOT NULL,
        MaNhanVien INT NULL,
        TrangThai INT NOT NULL,
        LyDo NVARCHAR(200) NULL,
        GhiChu NVARCHAR(1000) NULL,
        AnhBangChung NVARCHAR(500) NULL,
        ThoiGian DATETIME2 NOT NULL CONSTRAINT DF_LICHSUGIAOHANG_ThoiGian DEFAULT SYSDATETIME(),
        ThoiGianHenLai DATETIME2 NULL,
        CONSTRAINT FK_LICHSUGIAOHANG_DONHANG FOREIGN KEY (MaDonHang) REFERENCES dbo.DONHANG(MaDonHang),
        CONSTRAINT FK_LICHSUGIAOHANG_NHANVIEN FOREIGN KEY (MaNhanVien) REFERENCES dbo.NHANVIEN(MaNV)
    );
    CREATE INDEX IX_LICHSUGIAOHANG_DonHang_ThoiGian ON dbo.LICHSUGIAOHANG(MaDonHang, ThoiGian DESC);
    CREATE INDEX IX_LICHSUGIAOHANG_NhanVien_TrangThai ON dbo.LICHSUGIAOHANG(MaNhanVien, TrangThai);
END");
        }

        private bool TrySaveEvidence(HttpPostedFileBase file, int orderId, out string relativePath)
        {
            relativePath = null;
            if (file == null || file.ContentLength == 0) return true;
            if (file.ContentLength > 5 * 1024 * 1024) { TempData["ErrorMessage"] = "Ảnh bằng chứng không được vượt quá 5 MB."; return false; }
            var extension = Path.GetExtension(file.FileName)?.ToLowerInvariant();
            if (extension != ".jpg" && extension != ".jpeg" && extension != ".png" && extension != ".webp") { TempData["ErrorMessage"] = "Ảnh phải có định dạng JPG, PNG hoặc WEBP."; return false; }
            var folder = Server.MapPath("~/Uploads/DeliveryEvidence/" + orderId);
            Directory.CreateDirectory(folder);
            var fileName = Guid.NewGuid().ToString("N") + extension;
            file.SaveAs(Path.Combine(folder, fileName));
            relativePath = "/Uploads/DeliveryEvidence/" + orderId + "/" + fileName;
            return true;
        }

        private static string GetReasonName(DeliveryFailureReason reason)
        {
            var member = typeof(DeliveryFailureReason).GetMember(reason.ToString()).First();
            var display = (System.ComponentModel.DataAnnotations.DisplayAttribute)member.GetCustomAttributes(typeof(System.ComponentModel.DataAnnotations.DisplayAttribute), false).First();
            return display.Name;
        }

        protected override void Dispose(bool disposing) { if (disposing) db.Dispose(); base.Dispose(disposing); }
    }
}
