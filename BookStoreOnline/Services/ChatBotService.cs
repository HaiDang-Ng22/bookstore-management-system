using BookStoreOnline.Models;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Globalization;
using System.Text;

namespace BookStoreOnline.Services
{
    public class ChatBotService
    {
        private readonly NhaSachEntities3 db = new NhaSachEntities3();
        private string RemoveVietnamese(string text)
        {
            if (string.IsNullOrWhiteSpace(text))
                return "";

            text = text.ToLower();

            string normalized = text.Normalize(NormalizationForm.FormD);

            StringBuilder sb = new StringBuilder();

            foreach (char c in normalized)
            {
                UnicodeCategory uc = CharUnicodeInfo.GetUnicodeCategory(c);

                if (uc != UnicodeCategory.NonSpacingMark)
                    sb.Append(c);
            }

            return sb.ToString()
                     .Normalize(NormalizationForm.FormC)
                     .Replace('đ', 'd');
        }
        //=====================================================
        // HÀM CHUNG BUILD CARD
        //=====================================================
        private string BuildBookCards(List<SANPHAM> books)
        {
            if (!books.Any())
                return null;

            StringBuilder html = new StringBuilder();

            foreach (var item in books)
            {
                html.Append($@"
<div class='card mb-3 shadow-sm'>

    <img src='{item.Anh}'
         class='card-img-top'
         style='height:180px;object-fit:cover;'>

    <div class='card-body'>

        <h6>{item.TenSanPham}</h6>

        <p>👤 {item.TacGia}</p>

        <p class='text-danger fw-bold'>
            {item.Gia:N0} VNĐ
        </p>

        <a href='/ProductDetail/Index/{item.MaSanPham}'
           class='btn btn-danger btn-sm'>
            Xem chi tiết
        </a>

    </div>
</div>");
            }

            return html.ToString();
        }

        //=====================================================
        // SEARCH (Tên → Tác giả → Thể loại)
        //=====================================================
        public string SearchBook(string keyword)
        {
            keyword = RemoveVietnamese(keyword);

            var words = keyword
                .Split(' ')
                .Where(x => x.Length > 1)
                .Distinct()
                .ToList();

            var books = db.SANPHAMs
                .ToList()
                .Select(x =>
                {
                    int score = 0;

                    string ten = RemoveVietnamese(x.TenSanPham ?? "");
                    string tg = RemoveVietnamese(x.TacGia ?? "");
                    string loai = RemoveVietnamese(x.LOAI?.Tenloai ?? "");

                    foreach (var w in words)
                    {
                        if (ten.Contains(w))
                            score += 5;

                        if (tg.Contains(w))
                            score += 3;

                        if (loai.Contains(w))
                            score += 2;
                    }

                    return new
                    {
                        Book = x,
                        Score = score
                    };
                })
                .Where(x => x.Score > 0)
                .OrderByDescending(x => x.Score)
                .ThenByDescending(x => x.Book.SoLuongBan)
                .Take(6)
                .Select(x => x.Book)
                .ToList();

            return BuildBookCards(books);
        }

        //=====================================================
        // GIÁ
        //=====================================================
        public string GetBookByPrice(int maxPrice)
        {
            var books = db.SANPHAMs
                .Where(x => x.Gia <= maxPrice)
                .OrderBy(x => x.Gia)
                .Take(6)
                .ToList();

            return BuildBookCards(books);
        }

        //=====================================================
        // BEST SELLER
        //=====================================================
        public string GetBestSeller()
        {
            var books = db.SANPHAMs
                .OrderByDescending(x => x.SoLuongBan)
                .Take(6)
                .ToList();

            return BuildBookCards(books);
        }

        //=====================================================
        // THỂ LOẠI
        //=====================================================
        public string GetBookByCategory(string category)
        {
            var books = db.SANPHAMs
                .Where(x => x.LOAI.Tenloai.Contains(category))
                .Take(6)
                .ToList();

            return BuildBookCards(books);
        }

        //=====================================================
        // TÁC GIẢ
        //=====================================================
        public string GetBookByAuthor(string author)
        {
            var books = db.SANPHAMs
                .Where(x => x.TacGia.Contains(author))
                .Take(6)
                .ToList();

            return BuildBookCards(books);
        }
        //=========================================================
        // TRA CỨU ĐƠN HÀNG
        //=========================================================

        public string GetOrder(int maDonHang)
        {
            var order = db.DONHANGs.FirstOrDefault(x => x.MaDonHang == maDonHang);

            if (order == null)
                return "❌ Không tìm thấy đơn hàng.";

            string trangThai = "";

            switch (order.TrangThai)
            {
                case 0:
                    trangThai = "Chờ xác nhận";
                    break;

                case 1:
                    trangThai = "Đã xác nhận";
                    break;

                case 2:
                    trangThai = "Đang giao";
                    break;

                case 3:
                    trangThai = "Hoàn thành";
                    break;

                case 4:
                    trangThai = "Đã hủy";
                    break;

                default:
                    trangThai = "Không xác định";
                    break;
            }

            return
                $"📦 Đơn hàng #{order.MaDonHang}\n\n" +
                $"📅 Ngày đặt: {order.NgayDat:dd/MM/yyyy}\n" +
                $"💰 Tổng tiền: {order.TongTien:N0} VNĐ\n" +
                $"🚚 Trạng thái: {trangThai}";
        }
    }
}