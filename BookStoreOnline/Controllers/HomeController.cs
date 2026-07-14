using System;
using System.Linq;
using System.Web.Mvc;
using BookStoreOnline.Models;

namespace BookStoreOnline.Controllers
{
    public class HomeController : Controller
    {
        private readonly NhaSachEntities3 db = new NhaSachEntities3();

        // Gộp chung 2 hàm Index bị lỗi cú pháp thành 1 hàm duy nhất xử lý cả bộ lọc
        public ActionResult Index(int? minPrice, int? maxPrice, string sortOrder)
        {
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.SortOrder = sortOrder;

            var query = db.SANPHAMs.AsQueryable();

            // Thực hiện lọc theo giá
            if (minPrice.HasValue) query = query.Where(x => x.Gia >= minPrice.Value);
            if (maxPrice.HasValue) query = query.Where(x => x.Gia <= maxPrice.Value);

            // Sắp xếp dữ liệu
            if (sortOrder == "price_asc")
            {
                query = query.OrderBy(x => x.Gia);
            }
            else if (sortOrder == "price_desc")
            {
                query = query.OrderByDescending(x => x.Gia);
            }
            else
            {
                query = query.OrderByDescending(x => x.MaSanPham);
            }

            // Tính toán số lượng cần lấy: Có lọc/sắp xếp lấy 24, mặc định lấy 8
            int takeCount = (minPrice.HasValue || maxPrice.HasValue || !string.IsNullOrEmpty(sortOrder)) ? 24 : 8;
            var books = query.Take(takeCount).ToList();

            // Lấy 5 cuốn sách bán chạy nhất truyền qua ViewBag
            var topBooks = db.SANPHAMs.OrderByDescending(s => s.SoLuongBan).Take(5).ToList();
            ViewBag.TopBooks = topBooks;

            return View(books);
        }

        // Thêm hàm giả định này nếu trong dự án của bạn chưa định nghĩa, tránh lỗi biên dịch ở CartController
        public static void TargetInteraction(int maKH, int maSanPham, int loại, NhaSachEntities3 context)
        {
            // Logic lưu tương tác người dùng (nếu có)
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