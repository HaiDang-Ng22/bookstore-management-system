using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BookStoreOnline.Models;
using BookStoreOnline.Core;
using static BookStoreOnline.Areas.Admin.Constants.Constants;

namespace BookStoreOnline.Areas.Admin.Controllers
{
    [AdminAuthorize(AdminRole.Admin)]
    public class InventoryController : Controller
    {
        private NhaSachEntities3 db = new NhaSachEntities3();

        // GET: Admin/Inventory
        public ActionResult Index(string searchString)
        {
            var kho = db.SANPHAMs.AsQueryable();

            // Tính năng Tìm kiếm sách trong kho
            if (!string.IsNullOrEmpty(searchString))
            {
                kho = kho.Where(p => p.TenSanPham.Contains(searchString) || p.TacGia.Contains(searchString));
                ViewBag.CurrentFilter = searchString;
            }

            // Sắp xếp số lượng từ ít đến nhiều để ưu tiên cảnh báo hàng sắp hết trước
            var danhSachKho = kho.OrderBy(p => p.SoLuong).ToList();
            return View(danhSachKho);
        }

        // POST: Admin/Inventory/ImportStock (Nghiệp vụ Nhập Kho - Cộng dồn)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ImportStock(int maSanPham, int soLuongNhap)
        {
            if (soLuongNhap <= 0)
            {
                TempData["Error"] = "Số lượng nhập kho phải lớn hơn 0!";
                return RedirectToAction("Index");
            }

            var sanPham = db.SANPHAMs.Find(maSanPham);
            if (sanPham != null)
            {
                // Thực hiện cộng dồn số lượng vào kho hiện tại
                sanPham.SoLuong += soLuongNhap;
                db.Entry(sanPham).State = EntityState.Modified;
                db.SaveChanges();

                TempData["Success"] = $"Đã nhập thêm {soLuongNhap} cuốn cho sách '{sanPham.TenSanPham}' thành công!";
            }
            else
            {
                TempData["Error"] = "Không tìm thấy sản phẩm cần nhập kho.";
            }

            return RedirectToAction("Index");
        }

        // POST: Admin/Inventory/UpdateStock (Nghiệp vụ Sửa/Cập nhật trực tiếp số lượng)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateStock(int maSanPham, int soLuongMoi)
        {
            if (soLuongMoi < 0)
            {
                TempData["Error"] = "Số lượng tồn kho không được nhỏ hơn 0!";
                return RedirectToAction("Index");
            }

            var sanPham = db.SANPHAMs.Find(maSanPham);
            if (sanPham != null)
            {
                // Thay thế trực tiếp số lượng cũ bằng số lượng mới
                sanPham.SoLuong = soLuongMoi;
                db.Entry(sanPham).State = EntityState.Modified;
                db.SaveChanges();

                TempData["Success"] = $"Đã cập nhật số lượng tồn kho của sách '{sanPham.TenSanPham}' thành {soLuongMoi} cuốn!";
            }
            return RedirectToAction("Index");
        }

        // POST: Admin/Inventory/ClearStock (Nghiệp vụ Xóa số lượng kho - đưa về 0)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult ClearStock(int maSanPham)
        {
            var sanPham = db.SANPHAMs.Find(maSanPham);
            if (sanPham != null)
            {
                sanPham.SoLuong = 0; // Đưa số lượng về 0 thay vì xóa sản phẩm trong DB để tránh lỗi toàn vẹn dữ liệu
                db.Entry(sanPham).State = EntityState.Modified;
                db.SaveChanges();

                TempData["Success"] = $"Đã đưa số lượng kho của sách '{sanPham.TenSanPham}' về 0 (Hết hàng)!";
            }
            return RedirectToAction("Index");
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
}