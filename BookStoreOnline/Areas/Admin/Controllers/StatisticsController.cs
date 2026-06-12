using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
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
        public ActionResult Statistics()
        {
            // Lấy tất cả sản phẩm từ cơ sở dữ liệu
            var products = db.SANPHAMs.ToList();

            // Tạo danh sách thống kê
            var productStats = products.Select(p => new ProductStatViewModel
            {
                ProductName = p.TenSanPham,
                QuantityInStock = p.SoLuong, // Số lượng tồn kho
                QuantitySold = p.SoLuongBan, // Số lượng đã bán
                TotalRevenue = p.SoLuongBan * (p.Gia ?? 0) // Doanh thu
            }).ToList();

            // Tính tổng số lượng sách, số lượng bán và doanh thu
            var totalQuantityInStock = productStats.Sum(p => p.QuantityInStock);
            var totalQuantitySold = productStats.Sum(p => p.QuantitySold);
            var totalRevenue = productStats.Sum(p => p.TotalRevenue);

            // Truyền dữ liệu đến view
            ViewBag.ProductStats = productStats;
            ViewBag.TotalQuantityInStock = totalQuantityInStock;
            ViewBag.TotalQuantitySold = totalQuantitySold;
            ViewBag.TotalRevenue = totalRevenue;

            return View();
        }
    }

    public class ProductStatViewModel
    {
        public string ProductName { get; set; }
        public int QuantityInStock { get; set; }
        public int QuantitySold { get; set; }
        public decimal TotalRevenue { get; set; }
    }
}
