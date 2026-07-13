using BookStoreOnline.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web;
using System.Web.Mvc;

namespace BookStoreOnline.Controllers
{
    public class CategoryController : Controller
    {
        private NhaSachEntities3 db = new NhaSachEntities3();

        // Bỏ dấu tiếng Việt
        private string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            string normalizedString = text.Normalize(NormalizationForm.FormD);
            StringBuilder stringBuilder = new StringBuilder();

            foreach (char c in normalizedString)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString()
                .Replace('đ', 'd')
                .Replace('Đ', 'D')
                .Normalize(NormalizationForm.FormC);
        }

        // Hiển thị sách theo loại
        public ActionResult Index(int id)
        {
            ViewBag.CategoryName = db.LOAIs
                .FirstOrDefault(x => x.Maloai == id)?.Tenloai;

            var products = db.SANPHAMs
                .Where(x => x.MaLoai == id)
                .ToList();

            return View(products);
        }

        // Hiển thị tất cả sách
        public ActionResult GetAllBook()
        {
            return View(db.SANPHAMs.ToList());
        }

        // 1. CẢI TIẾN: TÌM KIẾM THÔNG MINH ĐA ĐIỀU KIỆN (CHẤP NHẬN TỪ KHÓA NGẮN)
        public ActionResult Search(string inputString, int? minPrice, int? maxPrice)
        {
            // Bảo mật: Mã hóa HTML an toàn chống lỗ hổng XSS
            ViewBag.TextSearch = HttpUtility.HtmlEncode(inputString);
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;

            // Tối ưu hiệu năng: Eager Loading kết hợp AsQueryable diệt lỗi N+1 Query
            var query = db.SANPHAMs.Include("LOAI").AsQueryable();

            // Lọc thô khoảng giá trước dưới Database SQL Server
            if (minPrice.HasValue) query = query.Where(x => x.Gia >= minPrice.Value);
            if (maxPrice.HasValue) query = query.Where(x => x.Gia <= maxPrice.Value);

            var rawProducts = query.ToList();

            if (string.IsNullOrWhiteSpace(inputString))
            {
                return View("Search", rawProducts);
            }

            // Chuẩn hóa từ khóa tìm kiếm đầu vào: Khử dấu, viết thường, xóa khoảng trắng
            string keyword = RemoveDiacritics(inputString.Trim().ToLower());

            // SỬA LỖI TẠI ĐÂY: Cho phép nhận từ khóa ngắn từ 1 ký tự trở lên (w.Length >= 1)
            var tokenWords = keyword.Split(' ').Where(w => w.Length >= 1).Distinct().ToList();

            // Thực thi giải thuật Chấm điểm chỉ mục trọng số văn bản cải tiến
            var filteredResult = rawProducts.Select(x =>
            {
                string tenTheLoai = "";
                if (x.LOAI != null)
                {
                    tenTheLoai = x.LOAI.Tenloai ?? "";
                }

                string tenSachKhongDau = RemoveDiacritics(x.TenSanPham ?? "").ToLower();
                string tacGiaKhongDau = RemoveDiacritics(x.TacGia ?? "").ToLower();
                string theLoaiKhongDau = RemoveDiacritics(tenTheLoai).ToLower();

                // Thuật toán tính toán điểm số trọng số linh hoạt
                int totalScore = 0;

                // 1. Kiểm tra khớp khít cả cụm từ người dùng nhập
                if (tenSachKhongDau.Contains(keyword)) totalScore += 10;

                // 2. CẢI TIẾN MỚI: Nếu tên sách BẮT ĐẦU bằng từ khóa (Ví dụ: "Do" trong "Doraemon"), thưởng ngay 15 điểm
                if (tenSachKhongDau.StartsWith(keyword)) totalScore += 15;

                // 3. Tính điểm tổng theo từng từ đơn lẻ trong mảng Token
                totalScore += tokenWords.Sum(w => tenSachKhongDau.Contains(w) ? 5 : 0);
                totalScore += tokenWords.Sum(w => tacGiaKhongDau.Contains(w) ? 3 : 0);
                totalScore += tokenWords.Sum(w => theLoaiKhongDau.Contains(w) ? 2 : 0);

                return new
                {
                    Product = x,
                    Score = totalScore
                };
            })
            .Where(x => x.Score > 0) // Giữ lại toàn bộ sản phẩm có điểm số lớn hơn 0
            .OrderByDescending(x => x.Score) // Đẩy sản phẩm trùng khớp cao nhất lên hàng đầu
            .Select(x => x.Product)
            .ToList();

            return View("Search", filteredResult);
        }

        // 2. BỔ SUNG: API KẾT XUẤT TỪ KHÓA GỢI Ý KHI ĐANG GÕ PHÍM (AUTO-SUGGESTION)
        [HttpGet]
        public JsonResult GetSuggestions(string term)
        {
            if (string.IsNullOrWhiteSpace(term))
                return Json(new List<object>(), JsonRequestBehavior.AllowGet);

            // Chuẩn hóa từ khóa tìm kiếm: Xóa khoảng trắng, viết thường và khử dấu tiếng Việt
            string cleanTerm = RemoveDiacritics(term.Trim().ToLower());

            // TỐI ƯU HIỆU NĂNG: Tải danh sách thô rút gọn về RAM máy chủ, ngắt theo dõi thực thể để giải phóng RAM
            var rawList = db.SANPHAMs
                .AsNoTracking()
                .Select(x => new {
                    x.MaSanPham,
                    x.TenSanPham,
                    x.TacGia,
                    x.Gia,
                    x.Anh
                })
                .ToList();

            // Thực hiện quét khử dấu thông minh trên bộ nhớ RAM để cam đoan gõ không dấu vẫn thả gợi ý xuống chuẩn xác
            var suggestions = rawList
                .Where(x => RemoveDiacritics(x.TenSanPham ?? "").ToLower().Contains(cleanTerm) ||
                            RemoveDiacritics(x.TacGia ?? "").ToLower().Contains(cleanTerm))
                .Take(5) // Khống chế giao diện tối đa hiển thị 5 sản phẩm khớp nhất
                .Select(x => new {
                    id = x.MaSanPham,
                    title = x.TenSanPham,
                    author = x.TacGia,
                    price = string.Format("{0:N0} đ", x.Gia),
                    img = x.Anh
                })
                .ToList();

            return Json(suggestions, JsonRequestBehavior.AllowGet);
        }

        // 3. BỔ SUNG: THUẬT TOÁN GỢI Ý SẢN PHẨM THÔNG MINH (RECOMMENDATION)
        [HttpGet]
        public JsonResult GetRelatedBooks(int productId)
        {
            var currentBook = db.SANPHAMs.Find(productId);
            if (currentBook == null) return Json(new List<object>(), JsonRequestBehavior.AllowGet);

            // Thuật toán lọc gợi ý: Tìm các cuốn sách cùng thể loại (MaLoai), loại trừ cuốn đang xem,
            // ưu tiên các cuốn có lượt mua cao (SoLuongBan giảm dần) làm sản phẩm gợi ý
            var relatedBooks = db.SANPHAMs
                .AsNoTracking()
                .Where(x => x.MaLoai == currentBook.MaLoai && x.MaSanPham != productId)
                .OrderByDescending(x => x.SoLuongBan)
                .Take(4) // Trả về tối đa 4 cuốn sách gợi ý liên quan nhất
                .ToList()
                .Select(x => new {
                    id = x.MaSanPham,
                    title = x.TenSanPham,
                    price = string.Format("{0:N0} đ", x.Gia),
                    img = x.Anh
                }).ToList();

            return Json(relatedBooks, JsonRequestBehavior.AllowGet);
        }
    }
}