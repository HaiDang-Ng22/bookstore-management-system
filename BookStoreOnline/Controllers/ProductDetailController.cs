using System;
using System.Linq;
using System.Web.Mvc;
using BookStoreOnline.Models;

namespace BookStoreOnline.Controllers
{
    public class ProductDetailController : Controller
    {
        private readonly NhaSachEntities3 _db = new NhaSachEntities3();
        private readonly ProductProxy _productProxy;

        public ProductDetailController()
        {
            _productProxy = new ProductProxy(_db); // Khởi tạo Proxy 1 lần
        }

        // GET: ProductDetail
        public ActionResult Index(int id)
        {
            // Lazy load sản phẩm chính qua Proxy
            var product = _productProxy.GetProduct(id);
            if (product == null)
            {
                return HttpNotFound();
            }

            // Lazy load sản phẩm liên quan qua Proxy
            var relatedProducts = _productProxy.GetRelatedProducts(id, 3);

            ViewBag.Book = product;
            ViewBag.MoreBook = relatedProducts;
            ViewBag.User = Session["TaiKhoan"] as KHACHHANG;

            ViewBag.Volumes = _db.Database.SqlQuery<VolumeDto>("SELECT MaTap, TenTap, SoLuong FROM TAP_SANPHAM WHERE MaSanPham = @p0", id).ToList();
            ViewBag.Categories = _db.Database.SqlQuery<string>("SELECT l.TenLoai FROM SANPHAM_LOAI sl JOIN LOAI l ON sl.MaLoai = l.MaLoai WHERE sl.MaSanPham = @p0", id).ToList();

            var user = Session["TaiKhoan"] as KHACHHANG;
            if (user != null)
            {
                // Gọi hàm tích lũy điểm xem sản phẩm (1 điểm)
                HomeController.TargetInteraction(user.MaKH, id, 1, _db);
            }
            return View(product);
        }

        [HttpGet]
        public ActionResult LoadReviews(int productId)
        {
            var reviews = _productProxy.GetReviews(productId);
            return PartialView("_ReviewsPartial", reviews); // Đúng tên file
        }

        [HttpPost]
        public ActionResult AddReview(int ProductID, string NoiDung, int SoSao)
        {
            var user = Session["TaiKhoan"] as KHACHHANG;
            if (user == null)
            {
                return RedirectToAction("Login", "User");
            }

            var review = new DANHGIA
            {
                MaKH = user.MaKH,
                MaSanPham = ProductID,
                NoiDung = NoiDung,
                NgayTao = DateTime.Now,
                SoSao = SoSao
            };

            _db.DANHGIAs.Add(review);
            _db.SaveChanges();

            return RedirectToAction("Index", new { id = ProductID });
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                _db.Dispose(); // Giải phóng DbContext
            }
            base.Dispose(disposing);
        }
    }
}