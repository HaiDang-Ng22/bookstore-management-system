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
    [AdminAuthorize(AdminRole.Administrator, AdminRole.Manager)]
    public class StatisticsController : Controller
    {
        private NhaSachEntities3 db = new NhaSachEntities3();

        // GET: Admin/Statistics
        public ActionResult Statistics(int? year)
        {
            int selectedYear = year ?? DateTime.Now.Year;
            ViewBag.SelectedYear = selectedYear;

            // -----------------------------------------------------------------
            // 1. LẤY DANH SÁCH ĐƠN HÀNG TRONG NĂM ĐƯỢC CHỌN
            // -----------------------------------------------------------------
            var ordersInYear = db.DONHANGs
                                 .Where(o => o.NgayDat.HasValue && o.NgayDat.Value.Year == selectedYear)
                                 .ToList();

            var orderIdsInYear = ordersInYear.Select(o => o.MaDonHang).ToList();

            // Thống kê doanh thu 12 Tháng
            var monthlyStats = new decimal[12];
            foreach (var order in ordersInYear)
            {
                int month = order.NgayDat.Value.Month;
                monthlyStats[month - 1] += ((decimal?)order.TongTien ?? 0);
            }
            ViewBag.MonthlyRevenue = monthlyStats;

            // Thống kê doanh thu 4 Quý
            var quarterlyStats = new decimal[4];
            quarterlyStats[0] = monthlyStats[0] + monthlyStats[1] + monthlyStats[2];    // Quý 1
            quarterlyStats[1] = monthlyStats[3] + monthlyStats[4] + monthlyStats[5];    // Quý 2
            quarterlyStats[2] = monthlyStats[6] + monthlyStats[7] + monthlyStats[8];    // Quý 3
            quarterlyStats[3] = monthlyStats[9] + monthlyStats[10] + monthlyStats[11]; // Quý 4
            ViewBag.QuarterlyRevenue = quarterlyStats;


            // -----------------------------------------------------------------
            // ĐƯA DỮ LIỆU VỀ RAM BẰNG TO-LIST TRƯỚC ĐỂ TRÁNH LỖI MAPPING ĐA TẦNG
            // -----------------------------------------------------------------
            var rawOrderDetails = db.CHITIETDONHANGs
                                    .Include(ct => ct.SANPHAM)
                                    .Include(ct => ct.SANPHAM.LOAI)
                                    .Where(ct => ct.MaDonHang.HasValue &&
                                    orderIdsInYear.Contains(ct.MaDonHang.Value))
                                    .ToList();

            var orderDetailsInYear = rawOrderDetails.Select(ct => new
            {
                MaSanPham = ct.MaSanPham,
                SoLuongXong = ct.SoLuong ?? 0,
                GiaBanXong = ct.SANPHAM?.Gia ?? 0,
                TenTheLoaiXong = ct.SANPHAM?.LOAI?.Tenloai ?? "Chưa rõ",
                TacGiaXong = ct.SANPHAM?.TacGia ?? "Chưa rõ"
            }).ToList();


            // -----------------------------------------------------------------
            // 2. THỐNG KÊ THEO SÁCH (Sửa lỗi gán ngược vị trí Sold và Revenue)
            // -----------------------------------------------------------------
            var productSalesGroup = orderDetailsInYear
                .GroupBy(ct => ct.MaSanPham)
                .ToDictionary(
                    g => g.Key,
                    g => new {
                        Sold = g.Sum(ct => ct.SoLuongXong),
                        Revenue = g.Sum(ct => (decimal)ct.SoLuongXong * ct.GiaBanXong)
                    }
                );

            var productStats = db.SANPHAMs.ToList().Select(p => {
                var hasSales = productSalesGroup.TryGetValue(p.MaSanPham, out var saleInfo);
                return new ProductStatViewModel
                {
                    ProductId = p.MaSanPham,
                    ProductName = p.TenSanPham,
                    QuantityInStock = p.SoLuong,
                    QuantitySold = hasSales ? saleInfo.Sold : 0,       // Đã sửa: Gán đúng Sold (int)
                    TotalRevenue = hasSales ? saleInfo.Revenue : 0     // Đã sửa: Gán đúng Revenue (decimal)
                };
            }).OrderByDescending(p => p.QuantitySold).ToList();

            ViewBag.ProductStats = productStats;


            // -----------------------------------------------------------------
            // 3. THỐNG KÊ THEO THỂ LOẠI (Sửa lỗi gán ngược vị trí Sold và Revenue)
            // -----------------------------------------------------------------
            var categorySalesGroup = orderDetailsInYear
                .GroupBy(ct => ct.TenTheLoaiXong)
                .ToDictionary(
                    g => g.Key,
                    g => new {
                        Sold = g.Sum(ct => ct.SoLuongXong),
                        Revenue = g.Sum(ct => (decimal)ct.SoLuongXong * ct.GiaBanXong)
                    }
                );

            var categoryStats = db.LOAIs.ToList().Select(c => {
                var hasSales = categorySalesGroup.TryGetValue(c.Tenloai, out var saleInfo);
                return new CategoryStatViewModel
                {
                    CategoryName = c.Tenloai,
                    TotalQuantitySold = hasSales ? saleInfo.Sold : 0,    // Đã sửa: Gán đúng Sold (int)
                    TotalRevenue = hasSales ? saleInfo.Revenue : 0      // Đã sửa: Gán đúng Revenue (decimal)
                };
            }).OrderByDescending(c => c.TotalRevenue).ToList();

            ViewBag.CategoryStats = categoryStats;


            // -----------------------------------------------------------------
            // 4. THỐNG KÊ THEO TÁC GIẢ / NHÀ XUẤT BẢN
            // -----------------------------------------------------------------
            var publisherStats = orderDetailsInYear
                .GroupBy(ct => ct.TacGiaXong)
                .Select(g => new PublisherStatViewModel
                {
                    PublisherName = g.Key,
                    TotalQuantitySold = g.Sum(ct => ct.SoLuongXong),
                    TotalRevenue = g.Sum(ct => (decimal)ct.SoLuongXong * ct.GiaBanXong)
                }).OrderByDescending(pub => pub.TotalRevenue).ToList();

            ViewBag.PublisherStats = publisherStats;


            // -----------------------------------------------------------------
            // 5. TỔNG QUAN CÁC THẺ CARD SỐ LIỆU THEO NĂM
            // -----------------------------------------------------------------
            ViewBag.TotalQuantityInStock = db.SANPHAMs.Sum(p => (int?)p.SoLuong) ?? 0;
            ViewBag.TotalQuantitySold = orderDetailsInYear.Sum(ct => ct.SoLuongXong);
            ViewBag.TotalRevenueInYear = ordersInYear.Sum(o => (decimal?)o.TongTien) ?? 0;

            return View();
        }
    }

    public class ProductStatViewModel
    {
        public int ProductId { get; set; }
        public string ProductName { get; set; }
        public int QuantityInStock { get; set; }
        public int QuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class CategoryStatViewModel
    {
        public string CategoryName { get; set; }
        public int TotalQuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }

    public class PublisherStatViewModel
    {
        public string PublisherName { get; set; }
        public int TotalQuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}