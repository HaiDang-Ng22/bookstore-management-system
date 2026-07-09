using System.Data.Entity;
using System.Web.Mvc;
using BookStoreOnline.Models;
using BookStoreOnline.Core;
using BookStoreOnline.Areas.Admin.Constants;
using System.Linq;
using System.Net;
using System.Data.SqlClient;

namespace BookStoreOnline.Controllers
{
    public class OrderController : Controller
    {
        private NhaSachEntities3 db = new NhaSachEntities3();

        // GET: Order
        private KHACHHANG GetAuthenticatedUser()
        {
            var cookie = Request.Cookies["AccessToken"];
            if (cookie == null)
            {
                return null;
            }

            string accessToken = cookie.Value;
            return db.KHACHHANGs.FirstOrDefault(k => k.AccessToken == accessToken);
        }

        // GET: Order
        public ActionResult Index()
        {
            var user = GetAuthenticatedUser();
            if (user == null)
            {
                return RedirectToAction("Login", "User");
            }

            var orders = db.DONHANGs
                .Where(o => o.ID == user.MaKH)
                .OrderByDescending(o => o.NgayDat)
                .ToList();
            return View(orders);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Delete(int id, string reason)
        {
            var user = GetAuthenticatedUser();
            if (user == null)
            {
                return RedirectToAction("Login", "User");
            }

            int canceled = (int)Constants.StatusOrder.Canceled;
            int paid = (int)Constants.StatusPayment.Paid;
            int refund = (int)Constants.StatusPayment.Refund;
            int notConfirmed = (int)Constants.StatusOrder.NoInform;

            var sql = @"
UPDATE dbo.DONHANG
SET TrangThai = @canceled,
    TrangThaiThanhToan = CASE 
        WHEN TrangThaiThanhToan = @paid THEN @refund 
        ELSE TrangThaiThanhToan 
    END
WHERE MaDonHang = @id 
  AND ID = @customerId 
  AND TrangThai = @notConfirmed";

            var affected = db.Database.ExecuteSqlCommand(
                sql,
                new SqlParameter("@canceled", canceled),
                new SqlParameter("@paid", paid),
                new SqlParameter("@refund", refund),
                new SqlParameter("@id", id),
                new SqlParameter("@customerId", user.MaKH),
                new SqlParameter("@notConfirmed", notConfirmed)
            );

            if (affected == 1)
            {
                new CustomerTypeService(db).UpdateCustomerType(user.MaKH);
                TempData["SuccessMessage"] = "Đơn hàng đã được hủy thành công. Khoản thanh toán online nếu có đã chuyển sang chờ hoàn tiền.";
            }
            else
            {
                TempData["ErrorMessage"] = "Không thể hủy đơn. Đơn hàng không tồn tại hoặc đã được người bán xác nhận.";
            }

            return RedirectToAction("Details", new { id = id });
        }
        public ActionResult Details(int id)
        {
            var user = GetAuthenticatedUser();
            if (user == null) return RedirectToAction("Login", "User");

            var order = db.DONHANGs
                .Include(o => o.CHITIETDONHANGs.Select(d => d.SANPHAM)) // Nạp thông tin sản phẩm
                .Include(o => o.KHACHHANG)
                .FirstOrDefault(o => o.MaDonHang == id && o.ID == user.MaKH);

            if (order == null)
            {
                return HttpNotFound();
            }

            return View(order);
        }

        /// <summary>
        /// Method called after order is completed/confirmed to update customer VIP status
        /// This can be called from admin area or order processing system
        /// </summary>
        [HttpPost]
        public ActionResult UpdateCustomerVIPStatus(int orderId)
        {
            var order = db.DONHANGs.Find(orderId);
            if (order == null || !order.ID.HasValue)
            {
                return Json(new { success = false, message = "Order not found" });
            }

            try
            {
                var customerService = new CustomerTypeService(db);
                customerService.UpdateCustomerType(order.ID.Value);
                return Json(new { success = true, message = "Customer VIP status updated successfully" });
            }
            catch
            {
                return Json(new { success = false, message = "Error updating VIP status" });
            }
        }

        /// <summary>
        /// Get customer VIP benefits information
        /// </summary>
        [HttpGet]
        public ActionResult GetCustomerVIPBenefits()
        {
            var user = GetAuthenticatedUser();
            if (user == null)
            {
                return RedirectToAction("Login", "User");
            }

            var customerService = new CustomerTypeService(db);
            var benefits = customerService.GetVIPBenefits(user.MaKH);

            return Json(benefits, JsonRequestBehavior.AllowGet);
        }
    }
}
