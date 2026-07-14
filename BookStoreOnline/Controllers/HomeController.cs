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
        [HttpGet]
        public ActionResult GetRecommend()
        {
            try
            {
                // 1. Kiểm tra xem người dùng đã đăng nhập chưa
                var customer = Session["TaiKhoan"] as KHACHHANG;

                if (customer != null)
                {
                    int maKH = customer.MaKH;

                    // 2. Tìm thể loại (Category) có số điểm (Score) cao nhất của khách hàng này
                    // Truy vấn trực tiếp từ bảng UserPreference bằng SQL thuần để tối ưu tốc độ
                    var topPreference = db.Database.SqlQuery<string>(@"
                SELECT TOP 1 PreferenceValue 
                FROM UserPreference 
                WHERE MaKH = @p0 AND PreferenceType = 'Category' AND Score > 0
                ORDER BY Score DESC, LastUpdated DESC", maKH).FirstOrDefault();

                    if (!string.IsNullOrEmpty(topPreference))
                    {
                        // Chuyển đổi giá trị PreferenceValue sang kiểu int để so khớp với MaLoai trong bảng SANPHAM
                        if (int.TryParse(topPreference, out int favoriteCategory))
                        {
                            // Lấy ra 4 sản phẩm thuộc thể loại yêu thích nhất này
                            var personalizedProducts = db.SANPHAMs
                                                         .Where(p => p.MaLoai == favoriteCategory)
                                                         .OrderBy(x => Guid.NewGuid()) // Xáo trộn ngẫu nhiên trong cùng thể loại đó để đổi mới giao diện
                                                         .Take(4)
                                                         .Select(p => new {
                                                             MaSanPham = p.MaSanPham,
                                                             TenSanPham = p.TenSanPham,
                                                             Anh = p.Anh,
                                                             Gia = p.Gia
                                                         })
                                                         .ToList();

                            // Nếu thể loại này có đủ sản phẩm, trả về ngay cho Front-end
                            if (personalizedProducts.Any())
                            {
                                // Thêm thuộc tính đánh dấu để lúc debug/báo cáo biết là hệ thống đang chạy thuật toán cá nhân hóa
                                ViewBag.RecommendType = "Personalized";
                                return Json(personalizedProducts, JsonRequestBehavior.AllowGet);
                            }
                        }
                    }
                }

                // 3. CƠ CHẾ DỰ PHÒNG (FALLBACK): Nếu chưa đăng nhập hoặc chưa có lịch sử tương tác
                // Hệ thống sẽ lấy ngẫu nhiên 4 sản phẩm bất kỳ như cũ để hiển thị
                var defaultProducts = db.SANPHAMs
                                        .OrderBy(x => Guid.NewGuid())
                                        .Take(4)
                                        .Select(p => new {
                                            MaSanPham = p.MaSanPham,
                                            TenSanPham = p.TenSanPham,
                                            Anh = p.Anh,
                                            Gia = p.Gia
                                        })
                                        .ToList();

                ViewBag.RecommendType = "Random";
                return Json(defaultProducts, JsonRequestBehavior.AllowGet);
            }
            catch
            {
                // Trả về mảng rỗng nếu có lỗi hệ thống xảy ra để giao diện không bị crash lỗi 500
                return Json(new object[] { }, JsonRequestBehavior.AllowGet);
            }
        }

        public static void TargetInteraction(int maKH, int maSanPham, int pointScore, NhaSachEntities3 dbContext)
        {
            try
            {
                // 1. Lấy MaLoai của sản phẩm hiện tại
                var product = dbContext.SANPHAMs.FirstOrDefault(s => s.MaSanPham == maSanPham);
                if (product == null || product.MaLoai == null) return;

                string maLoaiStr = product.MaLoai.ToString();

                // 2. Nếu là hành vi XEM (1 điểm), cập nhật hoặc thêm mới vào ProductViewHistory
                if (pointScore == 1)
                {
                    dbContext.Database.ExecuteSqlCommand(@"
                        IF EXISTS (SELECT 1 FROM ProductViewHistory WHERE MaKH = @p0 AND MaSanPham = @p1)
                            UPDATE ProductViewHistory 
                            SET ViewCount = ViewCount + 1, LastViewed = GETDATE() 
                            WHERE MaKH = @p0 AND MaSanPham = @p1
                        ELSE
                            INSERT INTO ProductViewHistory (MaKH, MaSanPham, ViewCount, LastViewed) 
                            VALUES (@p0, @p1, 1, GETDATE())", maKH, maSanPham);
                }

                // 3. Cập nhật hoặc thêm mới điểm sở thích vào UserPreference
                dbContext.Database.ExecuteSqlCommand(@"
                    IF EXISTS (SELECT 1 FROM UserPreference WHERE MaKH = @p0 AND PreferenceType = 'Category' AND PreferenceValue = @p1)
                        UPDATE UserPreference 
                        SET Score = Score + @p2, LastUpdated = GETDATE() 
                        WHERE MaKH = @p0 AND PreferenceType = 'Category' AND PreferenceValue = @p1
                    ELSE
                        INSERT INTO UserPreference (MaKH, PreferenceType, PreferenceValue, Score, LastUpdated) 
                        VALUES (@p0, 'Category', @p1, @p2, GETDATE())", maKH, maLoaiStr, pointScore);
            }
            catch
            {
                // Bảo lưu catch trống từ code cũ để tránh làm gián đoạn luồng chính của Cart
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
}