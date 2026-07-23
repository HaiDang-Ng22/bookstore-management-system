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
using BookStoreOnline.Common;

namespace BookStoreOnline.Areas.Admin.Controllers
{
    [AdminAuthorize(AdminRole.Admin)]
    public class OrdersAdminController : Controller
    {
        private NhaSachEntities3 db = new NhaSachEntities3();

        // GET: Admin/Orders
        public ActionResult Index(string status, int? page)
        {
            List<DONHANG> donHang;
            ViewBag.CurrentStatus = status;

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

            int pageSize = 7;
            int currentPage = page ?? 1;
            int totalCount = donHang.Count;
            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            if (currentPage > totalPages && totalPages > 0)
            {
                currentPage = totalPages;
            }

            var pagedDonHang = donHang.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList();

            ViewBag.CurrentPage = currentPage;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalCount = totalCount;

            return View(pagedDonHang);
        }

        // GET: Admin/Orders/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            var order = db.DONHANGs.FirstOrDefault(d => d.MaDonHang == id);
            if (order == null) return HttpNotFound();

            var detail = db.CHITIETDONHANGs.Where(d => d.MaDonHang == id).ToList();
            ViewBag.Detail = detail;
            ViewBag.Total = order.TongTien;

            var shippers = db.NHANVIENs
                .Where(nv => nv.Quyen == (int)AdminRole.Shipper && (nv.TrangThai ?? false))
                .OrderBy(nv => nv.Ten)
                .ToList();
            ViewBag.Shippers = new SelectList(shippers, "MaNV", "Ten", order.MaNVXuLy);

            return View(order);
        }

        // GET: Admin/Orders/Create
        public ActionResult Create()
        {
            ViewBag.IDCus = new SelectList(db.KHACHHANGs, "ID", "Ten");
            return View();
        }

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
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            DONHANG donHang = db.DONHANGs.Find(id);
            if (donHang == null) return HttpNotFound();
            ViewBag.IDCus = new SelectList(db.KHACHHANGs, "ID", "Ten", donHang.ID);
            return View(donHang);
        }

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
            ViewBag.IDCus = new SelectList(db.KHACHHANGs, "ID", "Ten", donHang.ID);
            return View(donHang);
        }

        // GET: Admin/Orders/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null) return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            DONHANG donHang = db.DONHANGs.Find(id);
            if (donHang == null) return HttpNotFound();
            return View(donHang);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            var details = db.CHITIETDONHANGs.Where(c => c.MaDonHang == id).ToList();
            db.CHITIETDONHANGs.RemoveRange(details);
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

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult CancelOrder(int id, string lyDoHuy)
        {
            var order = db.DONHANGs.FirstOrDefault(item => item.MaDonHang == id);
            if (order == null) return HttpNotFound();

            // Cập nhật trạng thái hủy
            order.TrangThai = (int)StatusOrder.Canceled;
            db.SaveChanges();

            // Gửi email thông báo
            if (order.ID.HasValue)
            {
                var khachHang = db.KHACHHANGs.FirstOrDefault(kh => kh.MaKH == order.ID.Value);
                if (khachHang != null && !string.IsNullOrEmpty(khachHang.Email))
                {
                    string tieuDe = $"[BookStoreOnline] Thông báo hủy đơn hàng #{order.MaDonHang}";
                    string hienThiLyDo = string.IsNullOrEmpty(lyDoHuy) ? "Không có lý do cụ thể" : lyDoHuy;

                    string noiDung = $@"
                        <div style='font-family: Arial, sans-serif; max-width: 600px; margin: 0 auto; border: 1px solid #dddddd; padding: 20px; border-radius: 8px;'>
                            <h2 style='color: #d9534f; margin-top: 0;'>Thông báo hủy đơn hàng</h2>
                            <p>Xin chào <strong>{khachHang.Ten}</strong>,</p>
                            <p>Đơn hàng <strong>#{order.MaDonHang}</strong> đặt ngày {order.NgayDat:dd/MM/yyyy} của bạn đã bị hủy trên hệ thống.</p>
                            <div style='background-color: #f2dede; color: #a94442; padding: 15px; border-radius: 4px; margin: 15px 0;'>
                                <strong>Lý do hủy:</strong> {hienThiLyDo}
                            </div>
                            <p>Nếu bạn có thắc mắc, vui lòng phản hồi lại email này.</p>
                        </div>";

                    EmailHelper.SendEmail(khachHang.Email, tieuDe, noiDung);
                }
            }

            TempData["Message"] = "Đã hủy đơn hàng thành công và gửi email thông báo!";
            return RedirectToAction("Index");
        }

        public ActionResult Shipping(int id, int? shipperId)
        {
            var order = db.DONHANGs.FirstOrDefault(item => item.MaDonHang == id);
            if (order != null)
            {
                order.TrangThai = (int)StatusOrder.Shipping;
                if (shipperId.HasValue)
                {
                    var shipper = db.NHANVIENs.FirstOrDefault(nv => nv.MaNV == shipperId.Value && nv.Quyen == (int)AdminRole.Shipper && (nv.TrangThai ?? false));
                    if (shipper != null) order.MaNVXuLy = shipper.MaNV;
                }
                db.SaveChanges();
            }
            return RedirectToAction("Details", new { id });
        }

        public ActionResult ShippingSuccess(int id)
        {
            var order = db.DONHANGs.FirstOrDefault(item => item.MaDonHang == id);
            if (order != null)
            {
                order.TrangThai = (int)StatusOrder.Received;
                if (order.PhuongThucThanhToan == (int)TypePayment.COD) order.TrangThaiThanhToan = (int)StatusPayment.Paid;
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