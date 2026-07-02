using System;
using System.Collections.Generic;
using System.Linq;
using System.Web.Mvc;
using BookStoreOnline.Models;
using static BookStoreOnline.Areas.Admin.Constants.Constants;
using BookStoreOnline.Core;

namespace BookStoreOnline.Areas.Admin.Controllers
{
    [AdminAuthorize(AdminRole.Admin)]
    public class Home_PageController : Controller
    {
        private NhaSachEntities3 db = new NhaSachEntities3();

        // GET: Admin/HomePage
        public ActionResult Index(string status, int? page)
        {
            // Đếm các thông số tổng quan
            ViewBag.SoLuongKhachHang = db.KHACHHANGs.Count();
            ViewBag.TongSanPham = db.SANPHAMs.Count();
            ViewBag.TongDonHang = db.DONHANGs.Count();
            ViewBag.TongLoai = db.LOAIs.Count();

            // Lưu trạng thái để đồng bộ filter nếu cần
            ViewBag.CurrentStatus = status;

            // Lấy danh sách đơn hàng từ cơ sở dữ liệu và sắp xếp mới nhất lên đầu
            List<DONHANG> donHang;
            if (!string.IsNullOrEmpty(status))
            {
                if (Enum.TryParse(status, out StatusOrder parsedStatusOrder))
                {
                    int parsedStatusOrderInt = (int)parsedStatusOrder;
                    donHang = db.DONHANGs.Where(x => x.TrangThai == parsedStatusOrderInt).OrderByDescending(x => x.NgayDat).ToList();
                }
                else
                {
                    donHang = db.DONHANGs.OrderByDescending(x => x.NgayDat).ToList();
                }
            }
            else
            {
                donHang = db.DONHANGs.OrderByDescending(x => x.NgayDat).ToList();
            }

            // --- XỬ LÝ PHÂN TRANG CHO TRANG CHỦ DASHBOARD ---
            int pageSize = 7; // Hiển thị 7 đơn hàng gần đây nhất mỗi trang
            int currentPage = page ?? 1;
            int totalCount = donHang.Count;
            int totalPages = (int)Math.Ceiling((double)totalCount / pageSize);

            if (currentPage > totalPages && totalPages > 0)
            {
                currentPage = totalPages;
            }

            // Gán danh sách đã phân trang vào ViewBag thay vì lấy ToList() toàn bộ
            ViewBag.DonHangs = donHang.Skip((currentPage - 1) * pageSize).Take(pageSize).ToList();

            // Truyền dữ liệu phân trang sang View
            ViewBag.CurrentPage = currentPage;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalCount;

            return View();
        }
    }
}