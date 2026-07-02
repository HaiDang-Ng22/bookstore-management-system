using System;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using BookStoreOnline.Core;
using BookStoreOnline.Models;
using static BookStoreOnline.Areas.Admin.Constants.Constants;

namespace BookStoreOnline.Areas.Shipper.Controllers
{
    [ShipperAuthorize]
    public class OrdersShipperController : Controller
    {
        private NhaSachEntities3 db = new NhaSachEntities3();

        private NHANVIEN CurrentShipper => Session["TaiKhoan"] as NHANVIEN;

        public ActionResult Index(string tab, int? page)
        {
            var shipper = CurrentShipper;
            if (shipper == null) return RedirectToAction("Login", "User", new { area = "" });

            ViewBag.CurrentTab = string.IsNullOrEmpty(tab) ? "delivering" : tab;

            int pageSize = 7;
            int currentPage = page ?? 1;

            if (ViewBag.CurrentTab == "available")
            {
                var available = db.DONHANGs
                    .Where(x => x.TrangThai == (int)StatusOrder.Informed && x.MaNVXuLy == null)
                    .OrderByDescending(x => x.NgayDat)
                    .ToList();

                ViewBag.TotalCount = available.Count;
                ViewBag.TotalPages = (int)Math.Ceiling((double)available.Count / pageSize);
                ViewBag.CurrentPage = currentPage;
                return View("Index", Paginate(available, currentPage, pageSize));
            }

            var delivering = db.DONHANGs
                .Where(x => x.MaNVXuLy == shipper.MaNV && x.TrangThai == (int)StatusOrder.Shipping)
                .OrderByDescending(x => x.NgayDat)
                .ToList();

            ViewBag.TotalCount = delivering.Count;
            ViewBag.TotalPages = (int)Math.Ceiling((double)delivering.Count / pageSize);
            ViewBag.CurrentPage = currentPage;
            return View(Paginate(delivering, currentPage, pageSize));
        }

        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);

            var shipper = CurrentShipper;
            var order = db.DONHANGs.FirstOrDefault(d => d.MaDonHang == id);
            if (order == null) return HttpNotFound();

            bool canView = (order.MaNVXuLy == shipper.MaNV && order.TrangThai == (int)StatusOrder.Shipping)
                || (order.TrangThai == (int)StatusOrder.Informed && order.MaNVXuLy == null);

            if (!canView)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xem đơn hàng này.";
                return RedirectToAction("Index");
            }

            ViewBag.Detail = db.CHITIETDONHANGs.Where(d => d.MaDonHang == id).ToList();
            ViewBag.Total = order.TongTien;
            return View(order);
        }

        public ActionResult AcceptOrder(int id)
        {
            var shipper = CurrentShipper;
            var order = db.DONHANGs.FirstOrDefault(x => x.MaDonHang == id);

            if (order == null)
            {
                TempData["ErrorMessage"] = "Không tìm thấy đơn hàng.";
                return RedirectToAction("Index", new { tab = "available" });
            }

            if (order.TrangThai != (int)StatusOrder.Informed)
            {
                TempData["ErrorMessage"] = "Đơn hàng không còn ở trạng thái chờ giao.";
                return RedirectToAction("Index", new { tab = "available" });
            }

            if (order.MaNVXuLy.HasValue && order.MaNVXuLy != shipper.MaNV)
            {
                TempData["ErrorMessage"] = "Đơn hàng đã được shipper khác nhận.";
                return RedirectToAction("Index", new { tab = "available" });
            }

            order.MaNVXuLy = shipper.MaNV;
            order.TrangThai = (int)StatusOrder.Shipping;
            db.SaveChanges();

            TempData["SuccessMessage"] = "Đã nhận đơn hàng thành công. Hãy giao hàng cho khách.";
            return RedirectToAction("Details", new { id });
        }

        public ActionResult DeliverySuccess(int id)
        {
            var shipper = CurrentShipper;
            var order = db.DONHANGs.FirstOrDefault(x => x.MaDonHang == id);

            if (order == null || order.MaNVXuLy != shipper.MaNV)
            {
                TempData["ErrorMessage"] = "Bạn không có quyền xác nhận đơn hàng này.";
                return RedirectToAction("Index");
            }

            if (order.TrangThai != (int)StatusOrder.Shipping)
            {
                TempData["ErrorMessage"] = "Đơn hàng không ở trạng thái đang giao.";
                return RedirectToAction("Index");
            }

            order.TrangThai = (int)StatusOrder.Received;

            if (order.PhuongThucThanhToan == (int)TypePayment.COD)
            {
                order.TrangThaiThanhToan = (int)StatusPayment.Paid;
            }

            db.SaveChanges();

            if (order.ID.HasValue)
            {
                var customerService = new CustomerTypeService(db);
                customerService.UpdateCustomerType(order.ID.Value);
            }

            TempData["SuccessMessage"] = "Xác nhận giao hàng thành công!";
            return RedirectToAction("Index");
        }

        private System.Collections.Generic.List<DONHANG> Paginate(
            System.Collections.Generic.List<DONHANG> items, int page, int pageSize)
        {
            if (page < 1) page = 1;
            return items.Skip((page - 1) * pageSize).Take(pageSize).ToList();
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing) db.Dispose();
            base.Dispose(disposing);
        }
    }
}
