using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using BookStoreOnline.Models;
using static BookStoreOnline.Areas.Admin.Constants.Constants;
using BookStoreOnline.Core;

namespace BookStoreOnline.Areas.Admin.Controllers
{
    [AdminAuthorize(AdminRole.Administrator, AdminRole.Manager, AdminRole.Seller)]
    public class OrdersAdminController : Controller
    {
        private NhaSachEntities3 db = new NhaSachEntities3();

        // GET: Admin/Orders
        public ActionResult Index(string status, int? page)
        {
            List<DONHANG> donHang;

            // LƯU TRẠNG THÁI HIỆN TẠI ĐỂ RA VIEW FILTER NÚT ACTIVE
            ViewBag.CurrentStatus = status;

            // 1. Lọc dữ liệu theo trạng thái
            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse(status, out StatusOrder parsedStatusOrder))
                {
                    int parsedStatusOrderInt = (int)parsedStatusOrder;
                    donHang = db.DONHANGs.Where(x => x.TrangThai == parsedStatusOrderInt).OrderByDescending(x => x.NgayDat).ToList();
                }
                else
                {
                    donHang = db.DONHANGs.OrderByDescending(x => x.NgayDat).ToList();
                }
            }
            else
            {
                donHang = db.DONHANGs.OrderByDescending(x => x.NgayDat).ToList();
            }

            // 2. Cấu hình phân trang
            int pageSize = 7; // Số lượng đơn hàng trên mỗi trang
            int currentPage = page ?? 1;
            int totalCount = donHang.Count;
            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            // Tránh trường hợp trang hiện tại lớn hơn tổng số trang khi đổi bộ lọc
            if (currentPage > totalPages && totalPages > 0)
            {
                currentPage = totalPages;
            }

            // Cắt danh sách theo trang hiện tại
            var pagedDonHang = donHang.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList();

            // Truyền các giá trị phân trang ra View
            ViewBag.CurrentPage = currentPage;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;

            return View(pagedDonHang);
        }

        // GET: Admin/Orders/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            var order = db.DONHANGs.FirstOrDefault(d => d.MaDonHang == id);
            if (order == null)
            {
                return HttpNotFound();
            }

            var detail = db.CHITIETDONHANGs.Where(d => d.MaDonHang == id).ToList();
            ViewBag.Detail = detail;
            ViewBag.Total = order.TongTien;
            return View(order);
        }

        // GET: Admin/Orders/Create
        public ActionResult Create()
        {
            ViewBag.IDCus = new SelectList(db.KHACHHANGs, "ID", "Ten");
            return View();
        }

        // POST: Admin/Orders/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "MaDonHang,DiaChi,TrangThai,NgayDat,ID")] DONHANG donHang)
        {
            if (ModelState.IsValid)
            {
                db.DONHANGs.Add(donHang);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.IDCus = new SelectList(db.KHACHHANGs, "ID", "Ten", donHang.ID);
            return View(donHang);
        }

        // GET: Admin/Orders/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DONHANG donHang = db.DONHANGs.Find(id);
            if (donHang == null)
            {
                return HttpNotFound();
            }
            ViewBag.IDCus = new SelectList(db.KHACHHANGs, "ID", "Ten", donHang.ID);
            return View(donHang);
        }

        // POST: Admin/Orders/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MaDonHang,DiaChi,TrangThai,NgayDat,ID")] DONHANG donHang)
        {
            if (ModelState.IsValid)
            {
                db.Entry(donHang).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }
            ViewBag.IDCus = new SelectList(db.KHACHHANGs, "ID", "Ten ", donHang.ID);
            return View(donHang);
        }

        // GET: Admin/Orders/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            DONHANG donHang = db.DONHANGs.Find(id);
            if (donHang == null)
            {
                return HttpNotFound();
            }
            return View(donHang);
        }

        // POST: Admin/Orders/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            // Xóa các chi tiết đơn hàng trước tránh lỗi ràng buộc khóa ngoại
            var details = db.CHITIETDONHANGs.Where(c => c.MaDonHang == id).ToList();
            db.CHITIETDONHANGs.RemoveRange(details);

            // Xóa đơn hàng
            DONHANG donHang = db.DONHANGs.Find(id);
            if (donHang != null)
            {
                db.DONHANGs.Remove(donHang);
                db.SaveChanges();
            }
            return RedirectToAction("Index", new { status = Request.QueryString["status"] });
        }

        public ActionResult ConfirmOrder(int id)
        {
            var order = db.DONHANGs.FirstOrDefault(item => item.MaDonHang == id);
            if (order != null)
            {
                order.TrangThai = (int)StatusOrder.Informed;
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        public ActionResult CancelOrder(int id)
        {
            var order = db.DONHANGs.FirstOrDefault(item => item.MaDonHang == id);
            if (order != null)
            {
                order.TrangThai = (int)StatusOrder.Canceled;
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        public ActionResult Shipping(int id)
        {
            var order = db.DONHANGs.FirstOrDefault(item => item.MaDonHang == id);
            if (order != null)
            {
                order.TrangThai = (int)StatusOrder.Shipping;
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }

        public ActionResult ShippingSuccess(int id)
        {
            var order = db.DONHANGs.FirstOrDefault(item => item.MaDonHang == id);
            if (order != null)
            {
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
            }
            return RedirectToAction("Index");
        }
    }
}