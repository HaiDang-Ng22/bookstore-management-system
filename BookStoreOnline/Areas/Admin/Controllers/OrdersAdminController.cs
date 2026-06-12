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
using static BookStoreOnline.Areas.Admin.Constants.Constants;

namespace BookStoreOnline.Areas.Admin.Controllers
{
    [AdminAuthorize(AdminRole.Administrator, AdminRole.Manager, AdminRole.Seller)]
    public class OrdersAdminController : Controller
    {
        private NhaSachEntities3 db = new NhaSachEntities3();


        
        // GET: Admin/Orders
        public ActionResult Index(string status)
        {
            List<DONHANG> donHang;

            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse(status, out StatusOrder parsedStatusOrder))
                {
                    int parsedStatusOrderInt = (int)parsedStatusOrder;
                    donHang = db.DONHANGs.Where(x => x.TrangThai == parsedStatusOrderInt).ToList();
                }
                else
                {
                    donHang = db.DONHANGs.ToList();
                }
            }
            else
            {
                // If no status is provided, return all orders
                donHang = db.DONHANGs.ToList();
            }
            return View(donHang);
        }

        // GET: Admin/Orders/Details/5
        public ActionResult Details(int id)
        {
            var detail = db.CHITIETDONHANGs.Where(d => d.MaDonHang == id).ToList();
            ViewBag.Detail = detail;
            var order = db.DONHANGs.FirstOrDefault(d => d.MaDonHang == id);
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
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
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
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
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

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            // Xóa các chi tiết đơn hàng trước
            var details = db.CHITIETDONHANGs.Where(c => c.MaDonHang == id).ToList();
            db.CHITIETDONHANGs.RemoveRange(details);

            // Xóa đơn hàng
            DONHANG donHang = db.DONHANGs.Find(id);
            if (donHang != null)
            {
                db.DONHANGs.Remove(donHang);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
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
                // Cập nhật trạng thái đơn hàng thành "Đã giao hàng"
                order.TrangThai = (int)StatusOrder.Received;

                // Nếu đơn hàng là COD, cập nhật trạng thái thanh toán thành "Đã thanh toán"
                if (order.PhuongThucThanhToan == (int)TypePayment.COD)
                {
                    order.TrangThaiThanhToan = (int)StatusPayment.Paid; // 1 là "Đã thanh toán"
                }

                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}
