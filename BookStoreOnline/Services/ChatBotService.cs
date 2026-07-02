using BookStoreOnline.Models;
using System.Linq;

namespace BookStoreOnline.Services
{
    public class ChatBotService
    {
        private readonly NhaSachEntities3 db = new NhaSachEntities3();

        public string GetBook(string keyword)
        {
            var books = db.SANPHAMs
                .Where(x => x.TenSanPham.Contains(keyword))
                .Take(5)
                .ToList();

            if (!books.Any())
                return null;

            string result = "📚 Các sách tìm được:\n\n";

            foreach (var item in books)
            {
                result +=
                    $"• {item.TenSanPham}\n" +
                    $"Giá: {item.Gia:#,##0} VNĐ\n\n";
            }

            return result;
        }
        public string GetBookByPrice(int maxPrice)
        {
            var books = db.SANPHAMs
                .Where(x => x.Gia != null && x.Gia <= maxPrice)
                .OrderBy(x => x.Gia)
                .Take(5)
                .ToList();

            if (!books.Any())
                return null;

            string result = $"📚 Các sách dưới {maxPrice:N0} VNĐ\n\n";

            foreach (var item in books)
            {
                result += $"📖 {item.TenSanPham}\n";
                result += $"💰 {item.Gia:N0} VNĐ\n\n";
            }

            return result;
        }
        public string GetBestSeller()
        {
            var books = db.SANPHAMs
                .OrderByDescending(x => x.SoLuongBan)
                .Take(5)
                .ToList();

            if (!books.Any())
                return null;

            string result = "🔥 TOP 5 SÁCH BÁN CHẠY\n\n";

            int i = 1;

            foreach (var item in books)
            {
                result += $"{i}. {item.TenSanPham}\n";
                result += $"Đã bán: {item.SoLuongBan}\n";
                result += $"Giá: {item.Gia:N0} VNĐ\n\n";

                i++;
            }

            return result;
        }
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
        public string GetBookByCategory(string category)
        {
            var books = db.SANPHAMs
                .Where(x => x.LOAI.Tenloai.Contains(category))
                .Take(5)
                .ToList();

            if (!books.Any())
                return null;

            string result = $"📚 Sách thuộc thể loại {category}\n\n";

            foreach (var item in books)
            {
                result += $"{item.TenSanPham}\n";
                result += $"{item.Gia:N0} VNĐ\n\n";
            }

            return result;
        }
        public string GetBookCard(string keyword)
        {
            var books = db.SANPHAMs
                .Where(x => x.TenSanPham.Contains(keyword))
                .Take(4)
                .ToList();

            if (!books.Any())
                return null;

            string html = "";

            foreach (var item in books)
            {
                html += $@"
<div class='card mb-2' style='width:100%'>

<img src='/Images/{item.Anh}'
class='card-img-top'
style='height:180px;object-fit:cover'>

<div class='card-body'>

<h6>{item.TenSanPham}</h6>

<p class='text-danger fw-bold'>
{item.Gia:N0} VNĐ
</p>

<a href='/Category/Details/{item.MaSanPham}'
class='btn btn-danger btn-sm'>

Xem chi tiết

</a>

</div>

</div>";
            }

            return html;
        }
    }
}