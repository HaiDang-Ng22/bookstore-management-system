using BookStoreOnline.Models;
using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using System.Web.Mvc;

namespace BookStoreOnline.Controllers
{
    public class CartController : Controller
    {
        private readonly NhaSachEntities3 db = new NhaSachEntities3();

        public const int PAYMENT_COD = 1;
        public const int PAYMENT_PAYPAL = 2;
        public const int PAYMENT_MOMO = 3;
        public const int PAYMENT_VIETQR = 4;
        public const int PAYMENT_VNPAY = 5;

        private class DbCartItem
        {
            public int MaSanPham { get; set; }
            public int SoLuong { get; set; }
        }

        public static void SyncCartOnLogin(int customerId, HttpContextBase context)
        {
            using (var dbContext = new NhaSachEntities3())
            {
                var sessionCart = context.Session["GioHang"] as List<CartItem> ?? new List<CartItem>();

                // ĐÃ SỬA: Lấy thêm điều kiện MaTap = 0 hoặc lấy đúng theo cấu trúc DB mới
                var dbCartItems = dbContext.Database.SqlQuery<DbCartItem>(
                    "SELECT MaSanPham, SoLuong FROM GIOHANG WHERE MaKH = @p0", customerId).ToList();

                foreach (var item in sessionCart)
                {
                    var existingDbItem = dbCartItems.FirstOrDefault(d => d.MaSanPham == item.ProductID);
                    if (existingDbItem != null)
                    {
                        var productInDb = dbContext.SANPHAMs.Find(item.ProductID);
                        int maxQty = productInDb?.SoLuong ?? 999;
                        int newQty = Math.Min(existingDbItem.SoLuong + item.Number, maxQty);

                        dbContext.Database.ExecuteSqlCommand(
                            "UPDATE GIOHANG SET SoLuong = @p0 WHERE MaKH = @p1 AND MaSanPham = @p2",
                            newQty, customerId, item.ProductID);
                    }
                    else
                    {
                        // ĐÃ SỬA: Thêm cột MaTap và gán giá trị mặc định là 0 (hoặc item.VolumeID nếu CartItem có lưu)
                        int insertVolumeId = item.VolumeID ?? 0;
                        dbContext.Database.ExecuteSqlCommand(
                            "INSERT GIOHANG (MaKH, MaSanPham, SoLuong, MaTap) VALUES (@p0, @p1, @p2, @p3)",
                            customerId, item.ProductID, item.Number, insertVolumeId);
                    }
                }

                var finalDbCartItems = dbContext.Database.SqlQuery<DbCartItem>(
                    "SELECT MaSanPham, SoLuong FROM GIOHANG WHERE MaKH = @p0", customerId).ToList();

                var newSessionCart = new List<CartItem>();
                foreach (var dbItem in finalDbCartItems)
                {
                    try
                    {
                        var cartItem = new CartItem(dbItem.MaSanPham)
                        {
                            Number = dbItem.SoLuong
                        };
                        newSessionCart.Add(cartItem);
                    }
                    catch
                    {
                        // Bỏ qua nếu sản phẩm bị xóa khỏi hệ thống
                    }
                }
                context.Session["GioHang"] = newSessionCart;
            }
        }

        public ActionResult Index()
        {
            return View();
        }

        private List<CartItem> GetCart()
        {
            if (Session["GioHang"] is List<CartItem> cart)
            {
                return cart;
            }

            cart = new List<CartItem>();
            Session["GioHang"] = cart;
            return cart;
        }

        private void SaveCart(List<CartItem> cart)
        {
            Session["GioHang"] = cart;
        }

        [HttpPost]
        public ActionResult AddToCart(FormCollection product)
        {
            var cart = GetCart();

            if (!int.TryParse(product["ProductID"], out var productId) ||
                !int.TryParse(product["Quantity"], out var quantity))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Dữ liệu không hợp lệ");
            }

            int? volumeId = null;
            if (int.TryParse(product["VolumeID"], out var vId))
            {
                volumeId = vId;
            }

            var productInDb = db.SANPHAMs.Find(productId);
            if (productInDb == null)
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại." });
                }
                return HttpNotFound("Product not found");
                return HttpNotFound("Sản phẩm không tồn tại");
            }

            var cartItem = cart.FirstOrDefault(p => p.ProductID == productId && p.VolumeID == volumeId);

            int maxQty = productInDb.SoLuong;
            if (volumeId.HasValue)
            {
                maxQty = db.Database.SqlQuery<int>("SELECT SoLuong FROM TAP_SANPHAM WHERE MaTap = @p0", volumeId.Value).FirstOrDefault();
            }

            if (cartItem == null)
            {
                if (quantity > maxQty)
                {
                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = false, message = "Quá số lượng tồn trong kho." });
                    }
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Quá số lượng tồn trong kho");
                }

                cartItem = new CartItem(productId, volumeId) { Number = quantity };
                cart.Add(cartItem);
            }
            else
            {
                if (cartItem.Number + quantity > maxQty)
                {
                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = false, message = "Quá số lượng tồn trong kho." });
                    }
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Quá số lượng tồn trong kho.");
                }

                cartItem.Number += quantity;
            }
            SaveCart(cart);

            var customer = Session["TaiKhoan"] as KHACHHANG;
            if (customer != null)
            {
                // Nếu không có tập (null), ta thay bằng giá trị số 0 (hoặc ID mặc định của hệ thống) để không bị lỗi NOT NULL trong DB
                object dbVolumeId = volumeId.HasValue ? (object)volumeId.Value : 0;

                db.Database.ExecuteSqlCommand(@"
                IF EXISTS (SELECT 1 FROM GIOHANG WHERE MaKH = @p0 AND MaSanPham = @p1 AND MaTap = @p3)
                    UPDATE GIOHANG SET SoLuong = @p2 WHERE MaKH = @p0 AND MaSanPham = @p1 AND MaTap = @p3
                ELSE
                    INSERT GIOHANG (MaKH, MaSanPham, SoLuong, MaTap) VALUES (@p0, @p1, @p2, @p3)",
                    customer.MaKH, productId, cartItem.Number, dbVolumeId);

                HomeController.TargetInteraction(customer.MaKH, productId, 3, db);
            }

            TempData["SuccessMessage"] = "Đã thêm sản phẩm vào giỏ hàng thành công!";

            if (Request.IsAjaxRequest())
            {
                return Json(new { success = true, totalNumber = GetTotalNumber() });
            }

            if (Request.UrlReferrer != null)
            {
                return Redirect(Request.UrlReferrer.ToString());
            }
            return RedirectToAction("Index", "Home");
        }

        public ActionResult AddSingleProduct(int id)
        {
            var cart = GetCart();
            const int quantity = 1;

            var productInDb = db.SANPHAMs.Find(id);
            if (productInDb == null)
            {
                if (Request.IsAjaxRequest())
                {
                    return Json(new { success = false, message = "Sản phẩm không tồn tại." }, JsonRequestBehavior.AllowGet);
                }
                return HttpNotFound("Sản phẩm không tồn tại");
            }

            var cartItem = cart.FirstOrDefault(p => p.ProductID == id && p.VolumeID == null);
            if (cartItem == null)
            {
                if (quantity > productInDb.SoLuong)
                {
                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = false, message = "Quá số lượng tồn trong kho." }, JsonRequestBehavior.AllowGet);
                    }
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Quá số lượng tồn trong kho.");
                }

                cartItem = new CartItem(id) { Number = quantity };
                cart.Add(cartItem);
            }
            else
            {
                if (cartItem.Number + quantity > productInDb.SoLuong)
                {
                    if (Request.IsAjaxRequest())
                    {
                        return Json(new { success = false, message = "Quá số lượng tồn trong kho." }, JsonRequestBehavior.AllowGet);
                    }
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Quá số lượng tồn trong kho.");
                }

                cartItem.Number += quantity;
            }
            SaveCart(cart);

            var customer = Session["TaiKhoan"] as KHACHHANG;
            if (customer != null)
            {
                // ĐÃ SỬA: Thay thế toàn bộ check IS NULL và giá trị chèn NULL bằng 0 để tránh lỗi NOT NULL của DB
                db.Database.ExecuteSqlCommand(@"
            IF EXISTS (SELECT 1 FROM GIOHANG WHERE MaKH = @p0 AND MaSanPham = @p1 AND MaTap = 0)
                UPDATE GIOHANG SET SoLuong = @p2 WHERE MaKH = @p0 AND MaSanPham = @p1 AND MaTap = 0
            ELSE
                INSERT GIOHANG (MaKH, MaSanPham, SoLuong, MaTap) VALUES (@p0, @p1, @p2, 0)",
                    customer.MaKH, id, cartItem.Number);

                HomeController.TargetInteraction(customer.MaKH, id, 3, db);
            }

            TempData["SuccessMessage"] = "Đã thêm sản phẩm vào giỏ hàng thành công!";

            if (Request.IsAjaxRequest())
            {
                return Json(new { success = true, totalNumber = GetTotalNumber() }, JsonRequestBehavior.AllowGet);
            }
            if (Request.UrlReferrer != null)
            {
                return Redirect(Request.UrlReferrer.ToString());
            }
            return RedirectToAction("Index", "Home");
        }

        public ActionResult Remove(int id, int? volumeId = null)
        {
            var cart = GetCart();
            var cartItem = cart.FirstOrDefault(p => p.ProductID == id && p.VolumeID == volumeId);
            if (cartItem != null)
            {
                cart.Remove(cartItem);
                SaveCart(cart);

                var customer = Session["TaiKhoan"] as KHACHHANG;
                if (customer != null)
                {
                    // ĐÃ SỬA: Xóa chính xác theo trạng thái NULL của MaTap
                    if (volumeId.HasValue)
                    {
                        db.Database.ExecuteSqlCommand(
                            "DELETE FROM GIOHANG WHERE MaKH = @p0 AND MaSanPham = @p1 AND MaTap = @p2",
                            customer.MaKH, id, volumeId.Value);
                    }
                    else
                    {
                        db.Database.ExecuteSqlCommand(
                            "DELETE FROM GIOHANG WHERE MaKH = @p0 AND MaSanPham = @p1 AND MaTap IS NULL",
                            customer.MaKH, id);
                    }
                }
            }
            return RedirectToAction("GetCartInfo");
        }

        private int GetTotalNumber()
        {
            var cart = GetCart();
            return cart.Sum(sp => sp.Number);
        }

        private decimal GetTotalPrice()
        {
            var cart = GetCart();
            return cart.Sum(sp => sp.FinalPrice());
        }

        public ActionResult GetCartInfo()
        {
            var cart = GetCart();

            if (cart == null || !cart.Any())
            {
                return RedirectToAction("NullCart");
            }

            ViewBag.TotalNumber = GetTotalNumber();
            ViewBag.TotalPrice = GetTotalPrice();
            return View(cart);
        }

        [HttpPost]
        public ActionResult Update(int productId, int quantity, int? volumeId = null)
        {
            if (quantity <= 0)
            {
                return Json(new { success = false, message = "Số lượng không hợp lệ." }, JsonRequestBehavior.AllowGet);
            }

            var product = db.SANPHAMs.Find(productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tìm thấy." }, JsonRequestBehavior.AllowGet);
            }

            int maxQty = product.SoLuong;
            if (volumeId.HasValue)
            {
                maxQty = db.Database.SqlQuery<int>("SELECT SoLuong FROM TAP_SANPHAM WHERE MaTap = @p0", volumeId.Value).FirstOrDefault();
            }

            if (quantity > maxQty)
            {
                return Json(new { success = false, message = "Quá số lượng tồn trong kho", validQuantity = maxQty }, JsonRequestBehavior.AllowGet);
            }

            var cart = GetCart();
            var cartItem = cart.FirstOrDefault(item => item.ProductID == productId && item.VolumeID == volumeId);

            if (cartItem != null)
            {
                cartItem.Number = quantity;
                SaveCart(cart);

                var customer = Session["TaiKhoan"] as KHACHHANG;
                if (customer != null)
                {
                    // ĐÃ SỬA: Đồng bộ chuẩn xuống DB khi nhấn tăng giảm số lượng ở giao diện giỏ hàng
                    if (volumeId.HasValue)
                    {
                        db.Database.ExecuteSqlCommand(
                            "UPDATE GIOHANG SET SoLuong = @p0 WHERE MaKH = @p1 AND MaSanPham = @p2 AND MaTap = @p3",
                            quantity, customer.MaKH, productId, volumeId.Value);
                    }
                    else
                    {
                        db.Database.ExecuteSqlCommand(
                            "UPDATE GIOHANG SET SoLuong = @p0 WHERE MaKH = @p1 AND MaSanPham = @p2 AND MaTap IS NULL",
                            quantity, customer.MaKH, productId);
                    }
                }

                decimal newTotalPrice = GetTotalPrice();
                var discountAmount = Session["DiscountAmount"] as decimal? ?? 0;
                newTotalPrice -= discountAmount;
                if (newTotalPrice < 0) newTotalPrice = 0;

                return Json(new { success = true, newTotalPrice = (int)Math.Round(newTotalPrice) }, JsonRequestBehavior.AllowGet);
            }

            return Json(new { success = false, message = "Sản phẩm không tồn tại trong giỏ hàng." }, JsonRequestBehavior.AllowGet);
        }

        public ActionResult CartPartial()
        {
            ViewBag.TotalNumber = GetTotalNumber();
            return PartialView();
        }

        public ActionResult NullCart()
        {
            return View();
        }

        [HttpPost]
        public ActionResult CheckStock(int productId, int quantity, int? volumeId = null)
        {
            var product = db.SANPHAMs.Find(productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Sản phẩm không tìm thấy." });
            }

            int maxQty = product.SoLuong;
            if (volumeId.HasValue)
            {
                maxQty = db.Database.SqlQuery<int>("SELECT SoLuong FROM TAP_SANPHAM WHERE MaTap = @p0", volumeId.Value).FirstOrDefault();
            }

            if (quantity > maxQty)
            {
                return Json(new { success = false, message = "Quá số lượng tồn trong kho" });
            }

            return Json(new { success = true });
        }

        [HttpPost]
        public ActionResult InsertOrder(FormCollection form)
        {
            var cartItems = GetCart();
            if (cartItems == null || !cartItems.Any())
            {
                return Json(new { success = false, message = "Giỏ hàng của bạn đang rỗng." });
            }

            var customer = Session["TaiKhoan"] as KHACHHANG;
            if (customer == null)
            {
                return Json(new { success = false, message = "Chưa đăng nhập hệ thống." });
            }

            string address = form["address"] ?? form["DiaChi"];
            string inputPaymentMethod = form["paymentMethod1"] ?? form["paymentMethod"];

            var finalPrice = Session["FinalPrice"] as decimal? ?? GetTotalPrice();
            var roundedFinalPrice = (int)Math.Round(finalPrice);

            int trangThaiThanhToan = 0;
            int phuongThucThanhToan = PAYMENT_COD;

            string selectedMethod = (inputPaymentMethod ?? "").ToLower().Trim();

            if (selectedMethod == "vietqr") phuongThucThanhToan = PAYMENT_VIETQR;
            else if (selectedMethod == "vnpay") phuongThucThanhToan = PAYMENT_VNPAY;
            else if (selectedMethod == "paypal") phuongThucThanhToan = PAYMENT_PAYPAL;

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var order = new DONHANG
                    {
                        ID = customer.MaKH,
                        NgayDat = DateTime.Now,
                        DiaChi = address,
                        TrangThai = 0,
                        TrangThaiThanhToan = trangThaiThanhToan,
                        PhuongThucThanhToan = phuongThucThanhToan,
                        TongTien = roundedFinalPrice,
                        MaKM = Session["MaKM"] as string
                    };

                    db.DONHANGs.Add(order);
                    db.SaveChanges();

                    foreach (var item in cartItems)
                    {
                        var product = db.SANPHAMs.Find(item.ProductID);
                        if (product == null)
                        {
                            return Json(new { success = false, message = "Không tìm thấy sản phẩm trong hệ thống." });
                        }

                        if (item.Number > product.SoLuong)
                        {
                            return Json(new { success = false, message = $"Sách '{product.TenSanPham}' đã hết hàng hoặc vượt tồn kho." });
                        }

                        var orderDetail = new CHITIETDONHANG
                        {
                            MaDonHang = order.MaDonHang,
                            MaSanPham = item.ProductID,
                            SoLuong = item.Number
                        };
                        db.CHITIETDONHANGs.Add(orderDetail);
                        db.SaveChanges();

                        if (item.VolumeID.HasValue)
                        {
                            db.Database.ExecuteSqlCommand("UPDATE CHITIETDONHANG SET MaTap = @p0 WHERE ID = @p1", item.VolumeID.Value, orderDetail.ID);
                            db.Database.ExecuteSqlCommand("UPDATE TAP_SANPHAM SET SoLuong = SoLuong - @p0 WHERE MaTap = @p1", item.Number, item.VolumeID.Value);
                        }
                        else
                        {
                            product.SoLuong -= item.Number;
                            product.SoLuongBan += item.Number;
                            db.Entry(product).State = EntityState.Modified;
                        }

                        HomeController.TargetInteraction(customer.MaKH, item.ProductID, 5, db);
                    }

                    db.SaveChanges();

                    // Xóa giỏ hàng người dùng trong DB
                    db.Database.ExecuteSqlCommand("DELETE FROM GIOHANG WHERE MaKH = @p0", customer.MaKH);

                    Session["GioHang"] = null;
                    Session["DiscountAmount"] = null;
                    Session["FinalPrice"] = null;
                    Session["MaKM"] = null;

                    transaction.Commit();

                    if (selectedMethod == "vnpay")
                    {
                        string vnpayUrl = Url.Action("PaymentWithVnPay", "Cart", new { id = order.MaDonHang });
                        return Json(new { success = true, paymentMethod = "vnpay", redirectUrl = vnpayUrl });
                    }
                    else if (selectedMethod == "vietqr")
                    {
                        return Json(new { success = true, paymentMethod = "vietqr", orderId = order.MaDonHang });
                    }

                    return Json(new { success = true, paymentMethod = "cod", redirectUrl = Url.Action("SuccessView", "Cart") });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return Json(new { success = false, message = "Lỗi hệ thống: " + (ex.InnerException?.InnerException?.Message ?? ex.Message) });
                }
            }
        }

        [HttpGet]
        public async Task<JsonResult> CheckOrderStatus(int orderId)
        {
            try
            {
                if (orderId <= 0)
                {
                    return Json(new { success = false, status = -1, message = "Mã đơn hàng không hợp lệ." }, JsonRequestBehavior.AllowGet);
                }

                var order = await db.DONHANGs.FirstOrDefaultAsync(o => o.MaDonHang == orderId);
                if (order == null)
                {
                    return Json(new { success = false, status = -1, message = "Không tìm thấy đơn hàng." }, JsonRequestBehavior.AllowGet);
                }

                if (order.TrangThaiThanhToan == 1)
                {
                    return Json(new { success = true, status = 1 }, JsonRequestBehavior.AllowGet);
                }

                string apiKey = System.Configuration.ConfigurationManager.AppSettings["SePay_ApiKey"];
                string accountNo = System.Configuration.ConfigurationManager.AppSettings["SePay_AccountNo"];

                if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(accountNo))
                {
                    return Json(new { success = false, status = 0, message = "Hệ thống chưa cấu hình đầy đủ thông số kết nối SePay." }, JsonRequestBehavior.AllowGet);
                }

                string apiUrl = $"https://my.sepay.vn/userapi/transactions/list?account_number={accountNo}&limit=10";

                using (HttpClient client = new HttpClient())
                {
                    client.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");

                    HttpResponseMessage response = await client.GetAsync(apiUrl);
                    if (response.IsSuccessStatusCode)
                    {
                        string rawJson = await response.Content.ReadAsStringAsync();
                        dynamic apiData = JsonConvert.DeserializeObject(rawJson);

                        if (apiData != null && apiData.transactions != null)
                        {
                            string chuoiCanQuet = "DH" + orderId;

                            foreach (var item in apiData.transactions)
                            {
                                string transactionContent = (string)item.transaction_content;

                                if (!string.IsNullOrEmpty(transactionContent))
                                {
                                    transactionContent = transactionContent.ToUpper().Replace(" ", "");

                                    if (transactionContent.Contains(chuoiCanQuet))
                                    {
                                        order.TrangThaiThanhToan = 1;
                                        db.Entry(order).State = EntityState.Modified;
                                        await db.SaveChangesAsync();

                                        return Json(new { success = true, status = 1 }, JsonRequestBehavior.AllowGet);
                                    }
                                }
                            }
                        }
                    }
                }

                return Json(new { success = true, status = order.TrangThaiThanhToan }, JsonRequestBehavior.AllowGet);
            }
            catch (Exception ex)
            {
                return Json(new { success = false, status = -1, message = "Lỗi hệ thống C#: " + ex.Message }, JsonRequestBehavior.AllowGet);
            }
        }

        public ActionResult SuccessView()
        {
            return View("SuccessView");
        }

        public ActionResult FailureView()
        {
            return View("FailureView");
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