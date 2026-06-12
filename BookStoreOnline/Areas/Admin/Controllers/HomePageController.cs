using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using BookStoreOnline.Models; // Đảm bảo bạn đã thêm namespace cho mô hình dữ liệu của bạn
using static BookStoreOnline.Areas.Admin.Constants.Constants;

using BookStoreOnline.Core;
using static BookStoreOnline.Areas.Admin.Constants.Constants;

namespace BookStoreOnline.Areas.Admin.Controllers
{
    [AdminAuthorize(AdminRole.Administrator, AdminRole.Manager, AdminRole.Seller)]
    public class Home_PageController : Controller
    {
        private NhaSachEntities3 db = new NhaSachEntities3();
        private string status;

        // GET: Admin/HomePage
        public ActionResult Index()
        {
            // Đếm tổng số khách hàng
            var soLuongKhachHang = db.KHACHHANGs.Count();


            // Gửi thông tin số lượng khách hàng tới view
            ViewBag.SoLuongKhachHang = soLuongKhachHang;
            ViewBag.TongSanPham = db.SANPHAMs.Count(); // Tổng số sản phẩm
            ViewBag.TongDonHang = db.DONHANGs.Count(); // Tổng số đơn hàng
            ViewBag.TongLoai = db.LOAIs.Count();//
                                                // Lấy danh sách đơn hàng từ cơ sở dữ liệu
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
                    donHang = db.DONHANGs.ToList();  // Lấy tất cả đơn hàng nếu trạng thái không hợp lệ
                }
            }
            else
            {
                donHang = db.DONHANGs.ToList();  // Lấy tất cả đơn hàng nếu không có trạng thái
            }

            // Truyền vào ViewBag
            ViewBag.DonHangs = donHang;

            return View();
        }
    }
}