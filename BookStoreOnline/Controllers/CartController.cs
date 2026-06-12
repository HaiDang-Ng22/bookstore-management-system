using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using PayPal.Api;
using BookStoreOnline.Models;
using System.Web.Util;
using System.Threading.Tasks;

namespace BookStoreOnline.Controllers
{
    public class CartController : Controller
    {
        private readonly NhaSachEntities3 db = new NhaSachEntities3();

        private class DbCartItem
        {
            public int MaSanPham { get; set; }
            public int SoLuong { get; set; }
        }

        public static void SyncCartOnLogin(int customerId, HttpContextBase context)
        {
            using (var dbContext = new NhaSachEntities3())
            {
                // 1. Get cart items currently in session
                var sessionCart = context.Session["GioHang"] as List<CartItem> ?? new List<CartItem>();

                // 2. Get cart items from database
                var dbCartItems = dbContext.Database.SqlQuery<DbCartItem>(
                    "SELECT MaSanPham, SoLuong FROM GIOHANG WHERE MaKH = @p0", customerId).ToList();

                // 3. Merge session cart into database
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
                        dbContext.Database.ExecuteSqlCommand(
                            "INSERT INTO GIOHANG (MaKH, MaSanPham, SoLuong) VALUES (@p0, @p1, @p2)",
                            customerId, item.ProductID, item.Number);
                    }
                }

                // 4. Reload combined cart from database to session
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
                        // Ignore if product was deleted
                    }
                }
                context.Session["GioHang"] = newSessionCart;
            }
        }

        // GET: Cart
        public ActionResult Index()
        {
            return View();
        }

        // Get the current cart from session or create a new one if it doesn't exist
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

        // Save the cart to the session
        private void SaveCart(List<CartItem> cart)
        {
            Session["GioHang"] = cart;
        }

        // Add product to cart
        [HttpPost]
        public ActionResult AddToCart(FormCollection product)
        {
            var cart = GetCart();

            if (!int.TryParse(product["ProductID"], out var productId) ||
                !int.TryParse(product["Quantity"], out var quantity))
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Invalid input");
            }

            var productInDb = db.SANPHAMs.Find(productId);
            if (productInDb == null)
            {
                return HttpNotFound("Product not found");
            }

            var cartItem = cart.FirstOrDefault(p => p.ProductID == productId);
            if (cartItem == null)
            {
                if (quantity > productInDb.SoLuong)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Quá số lượng tồn trong kho");
                }

                cartItem = new CartItem(productId)
                {
                    Number = quantity
                };
                cart.Add(cartItem);
            }
            else
            {
                if (cartItem.Number + quantity > productInDb.SoLuong)
                {
                    return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Quá số lượng tồn trong kho.");
                }

                cartItem.Number += quantity;
            }
            SaveCart(cart);

            // Sync to DB
            var customer = Session["TaiKhoan"] as KHACHHANG;
            if (customer != null)
            {
                db.Database.ExecuteSqlCommand(@"
                    IF EXISTS (SELECT 1 FROM GIOHANG WHERE MaKH = @p0 AND MaSanPham = @p1)
                        UPDATE GIOHANG SET SoLuong = @p2 WHERE MaKH = @p0 AND MaSanPham = @p1
                    ELSE
                        INSERT INTO GIOHANG (MaKH, MaSanPham, SoLuong) VALUES (@p0, @p1, @p2)",
                    customer.MaKH, productId, cartItem.Number);
            }

            return RedirectToAction("GetCartInfo");
        }

        // Add a single product to the cart
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
                return HttpNotFound("Product not found");
            }

            var cartItem = cart.FirstOrDefault(p => p.ProductID == id);
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

                cartItem = new CartItem(id)
                {
                    Number = quantity
                };
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

            // Sync to DB
            var customer = Session["TaiKhoan"] as KHACHHANG;
            if (customer != null)
            {
                db.Database.ExecuteSqlCommand(@"
                    IF EXISTS (SELECT 1 FROM GIOHANG WHERE MaKH = @p0 AND MaSanPham = @p1)
                        UPDATE GIOHANG SET SoLuong = @p2 WHERE MaKH = @p0 AND MaSanPham = @p1
                    ELSE
                        INSERT INTO GIOHANG (MaKH, MaSanPham, SoLuong) VALUES (@p0, @p1, @p2)",
                    customer.MaKH, id, cartItem.Number);
            }

            if (Request.IsAjaxRequest())
            {
                return Json(new { success = true, totalNumber = GetTotalNumber() }, JsonRequestBehavior.AllowGet);
            }
            return RedirectToAction("GetCartInfo");
        }

        // Remove a product from the cart
        public ActionResult Remove(int id)
        {
            var cart = GetCart();
            var cartItem = cart.FirstOrDefault(p => p.ProductID == id);
            if (cartItem != null)
            {
                cart.Remove(cartItem);
                SaveCart(cart);

                // Sync to DB
                var customer = Session["TaiKhoan"] as KHACHHANG;
                if (customer != null)
                {
                    db.Database.ExecuteSqlCommand(
                        "DELETE FROM GIOHANG WHERE MaKH = @p0 AND MaSanPham = @p1",
                        customer.MaKH, id);
                }
            }
            return RedirectToAction("GetCartInfo");
        }

        // Get total number of items in the cart
        private int GetTotalNumber()
        {
            var cart = GetCart();
            return cart.Sum(sp => sp.Number);
        }

        // Get total price of items in the cart
        private decimal GetTotalPrice()
        {
            var cart = GetCart();
            return cart.Sum(sp => sp.FinalPrice());
        }

        // Display cart information
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

        // Update the quantity of a product in the cart
        [HttpPost]
        public ActionResult Update(int productId, int quantity)
        {
            // Kiểm tra nếu số lượng không hợp lệ
            if (quantity <= 0)
            {
                return Json(new { success = false, message = "Invalid quantity." }, JsonRequestBehavior.AllowGet);
            }

            // Tìm kiếm sản phẩm trong cơ sở dữ liệu
            var product = db.SANPHAMs.Find(productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found." }, JsonRequestBehavior.AllowGet);
            }

            // Kiểm tra nếu số lượng yêu cầu lớn hơn số lượng tồn kho
            if (quantity > product.SoLuong)
            {
                return Json(new { success = false, message = "Quá số lượng tồn trong kho", validQuantity = 1 }, JsonRequestBehavior.AllowGet);
            }

            // Lấy giỏ hàng từ session
            var cart = GetCart();
            var cartItem = cart.FirstOrDefault(item => item.ProductID == productId);

            if (cartItem != null)
            {
                // Cập nhật số lượng của sản phẩm trong giỏ hàng
                cartItem.Number = quantity;
                SaveCart(cart); // Lưu giỏ hàng vào session hoặc cơ sở dữ liệu

                // Sync to DB
                var customer = Session["TaiKhoan"] as KHACHHANG;
                if (customer != null)
                {
                    db.Database.ExecuteSqlCommand(
                        "UPDATE GIOHANG SET SoLuong = @p0 WHERE MaKH = @p1 AND MaSanPham = @p2",
                        quantity, customer.MaKH, productId);
                }

                return Json(new { success = true }, JsonRequestBehavior.AllowGet);
            }
            else
            {
                return Json(new { success = false, message = "Product not found in the cart." }, JsonRequestBehavior.AllowGet);
            }
        }

        // Partial view for cart summary
        public ActionResult CartPartial()
        {
            ViewBag.TotalNumber = GetTotalNumber();
            return PartialView();
        }

        // View for empty cart
        public ActionResult NullCart()
        {
            return View();
        }
        [HttpPost]
        public ActionResult CheckStock(int productId, int quantity)
        {
            var product = db.SANPHAMs.Find(productId);
            if (product == null)
            {
                return Json(new { success = false, message = "Product not found." });
            }

            if (quantity > product.SoLuong)
            {
                return Json(new { success = false, message = "Quá số lượng tồn trong kho" });
            }

            return Json(new { success = true });
        }
        [HttpPost]
        public ActionResult InsertOrder(string address, string paymentMethod1)
        {
            var cartItems = GetCart();
            if (cartItems == null || !cartItems.Any())
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Empty cart.");
            }

            var customer = Session["TaiKhoan"] as KHACHHANG;
            if (customer == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.Unauthorized, "Not logged in.");
            }

            var discountAmount = Session["DiscountAmount"] as decimal? ?? 0;
            var finalPrice = Session["FinalPrice"] as decimal? ?? GetTotalPrice();
            var roundedFinalPrice = (int)Math.Round(finalPrice);

            // Xử lý phương thức thanh toán
            int trangThaiThanhToan = 0;  // Mặc định là chưa thanh toán (COD)
            int phuongThucThanhToan = 0; // Mặc định là COD
            if (paymentMethod1 == "paypal")
            {
                phuongThucThanhToan = 2;  // PayPal
                trangThaiThanhToan = 1;   // Đã thanh toán
            }
            else if (paymentMethod1 == "cod")
            {
                phuongThucThanhToan = 1;  // Tiền mặt (COD)
                trangThaiThanhToan = 0;   // Chưa thanh toán
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var order = new DONHANG
                    {
                        ID = customer.MaKH,
                        NgayDat = DateTime.Now,
                        DiaChi = address,
                        TrangThai = 0, // Chờ xác nhận
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
                            return HttpNotFound("Product not found.");
                        }

                        if (item.Number > product.SoLuong)
                        {
                            return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Quá số lượng tồn trong kho.");
                        }

                        var orderDetail = new CHITIETDONHANG
                        {
                            MaDonHang = order.MaDonHang,
                            MaSanPham = item.ProductID,
                            SoLuong = item.Number
                        };
                        db.CHITIETDONHANGs.Add(orderDetail);

                        product.SoLuong -= item.Number;
                        product.SoLuongBan += item.Number;
                        db.Entry(product).State = EntityState.Modified;
                    }

                    db.SaveChanges();
                    
                    // Clear database cart
                    if (customer != null)
                    {
                        db.Database.ExecuteSqlCommand("DELETE FROM GIOHANG WHERE MaKH = @p0", customer.MaKH);
                    }

                    Session["GioHang"] = null;
                    Session["DiscountAmount"] = null;
                    Session["FinalPrice"] = null;
                    Session["MaKM"] = null;

                    transaction.Commit();

                    return RedirectToAction("SuccessView");
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Order processing error: " + ex.Message);
                }
            }
        }
        [HttpPost]
        public ActionResult InsertOrder1(string address, string paymentMethod1)
        {
            var cartItems = GetCart();
            if (cartItems == null || !cartItems.Any())
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Empty cart.");
            }

            var customer = Session["TaiKhoan"] as KHACHHANG;
            if (customer == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.Unauthorized, "Not logged in.");
            }

            var discountAmount = Session["DiscountAmount"] as decimal? ?? 0;
            var finalPrice = Session["FinalPrice"] as decimal? ?? GetTotalPrice();
            var roundedFinalPrice = (int)Math.Round(finalPrice);

            // Xử lý phương thức thanh toán
            int trangThaiThanhToan = 0;  // Mặc định là chưa thanh toán (COD)
            int phuongThucThanhToan = 0; // Mặc định là COD
            if (paymentMethod1 == "paypal")
            {
                phuongThucThanhToan = 2;  // Ngân hàng
                trangThaiThanhToan = 1;   // Đã thanh toán
            }
            else if (paymentMethod1 == "cod") //tienmat
            {
                phuongThucThanhToan = 1;
                trangThaiThanhToan = 1;
            }

            using (var transaction = db.Database.BeginTransaction())
            {
                try
                {
                    var order = new DONHANG
                    {
                        ID = customer.MaKH,
                        NgayDat = DateTime.Now,
                        DiaChi = address,
                        TrangThai = 0, // Not confirmed
                        TrangThaiThanhToan = trangThaiThanhToan, // Cập nhật trạng thái thanh toán
                        PhuongThucThanhToan = phuongThucThanhToan,  // Lưu phương thức thanh toán
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
                            return HttpNotFound("Product not found.");
                        }

                        if (item.Number > product.SoLuong)
                        {
                            return new HttpStatusCodeResult(HttpStatusCode.BadRequest, "Quá số lượng tồn trong kho.");
                        }

                        var orderDetail = new CHITIETDONHANG
                        {
                            MaDonHang = order.MaDonHang,
                            MaSanPham = item.ProductID,
                            SoLuong = item.Number
                        };
                        db.CHITIETDONHANGs.Add(orderDetail);

                        product.SoLuong -= item.Number;
                        product.SoLuongBan += item.Number;
                        db.Entry(product).State = EntityState.Modified;
                    }

                    db.SaveChanges();
                    // Do not clear cart until PayPal payment is confirmed
                    // Clear database cart
                    if (customer != null)
                    {
                        db.Database.ExecuteSqlCommand("DELETE FROM GIOHANG WHERE MaKH = @p0", customer.MaKH);
                    }

                    Session["GioHang"] = null;
                    Session["DiscountAmount"] = null;
                    Session["FinalPrice"] = null;
                    Session["MaKM"] = null;

                    transaction.Commit();

                    return RedirectToAction("momo", "Cart", new { id = order.MaDonHang });
                }
                catch (Exception ex)
                {
                    transaction.Rollback();
                    return new HttpStatusCodeResult(HttpStatusCode.InternalServerError, "Order processing error.");
                }
            }
        }

        [HttpPost]
        public JsonResult ApplyDiscount(string discountCode)
        {
            var discount = db.KHUYENMAIs.FirstOrDefault(d => d.MaKM == discountCode && d.KichHoat == true);
            decimal discountAmount = 0;
            decimal totalPrice = GetTotalPrice(); // Get the current total price before discount

            if (discount != null)
            {
                if (totalPrice >= discount.SoTienMuaHangToiThieu)
                {
                    discountAmount = discount.SoTienKM;
                    totalPrice -= discountAmount; // Apply discount
                    Session["DiscountAmount"] = discountAmount;
                    Session["FinalPrice"] = totalPrice;
                    Session["MaKM"] = discount.MaKM; // Lưu mã khuyến mãi
                }
                else
                {
                    return Json(new { success = false, message = "Không đạt yêu cầu tối thiểu để áp dụng mã khuyến mãi." });
                }
            }
            else
            {
                return Json(new { success = false, message = "Mã khuyến mãi không hợp lệ hoặc đã hết hạn" });
            }

            return Json(new { success = true, discountAmount = discountAmount, finalPrice = totalPrice });
        }
        public ActionResult FailureView()
        {
            return View();
        }
        public ActionResult SuccessView()
        {
            return View();
        }

        public ActionResult PaymentWithPaypal(string Cancel = null)
        {
            //getting the apiContext  
            APIContext apiContext = PaypalConfiguration.GetAPIContext();
            try
            {

                string payerId = Request.Params["PayerID"];
                if (string.IsNullOrEmpty(payerId))
                {

                    string baseURI = Request.Url.Scheme + "://" + Request.Url.Authority + "/cart/PaymentWithPayPal?";

                    var guid = Convert.ToString((new Random()).Next(100000));

                    var createdPayment = this.CreatePayment(apiContext, baseURI + "guid=" + guid);
                    var links = createdPayment.links.GetEnumerator();
                    string paypalRedirectUrl = null;
                    while (links.MoveNext())
                    {
                        Links lnk = links.Current;
                        if (lnk.rel.ToLower().Trim().Equals("approval_url"))
                        {
                            paypalRedirectUrl = lnk.href;
                        }
                    }
                    // saving the paymentID in the key guid  
                    Session.Add(guid, createdPayment.id);
                    return Redirect(paypalRedirectUrl);
                }
                else
                {
                    // This function exectues after receving all parameters for the payment  
                    var guid = Request.Params["guid"];
                    var executedPayment = ExecutePayment(apiContext, payerId, Session[guid] as string);
                    //If executed payment failed then we will show payment failure message to user  
                    if (executedPayment.state.ToLower() != "approved")
                    {
                        return View("FailureView");
                    }
                }
            }
            catch (Exception ex)
            {
                return View("FailureView");
            }
            //on successful payment, show success page to user.  
            return View("SuccessView");
        }
        private PayPal.Api.Payment payment;
        private Payment ExecutePayment(APIContext apiContext, string payerId, string paymentId)
        {
            var paymentExecution = new PaymentExecution()
            {
                payer_id = payerId
            };
            this.payment = new Payment()
            {
                id = paymentId
            };
            return this.payment.Execute(apiContext, paymentExecution);
        }
      
        private Payment CreatePayment(APIContext apiContext, string redirectUrl)
        {
            List<CartItem> listSanPham = Session["GioHang"] as List<CartItem>;
            var itemList = new ItemList()
            {
                items = new List<Item>()
            };

            decimal subtotal = 0;
            if (listSanPham != null)
            {
                foreach (var item in listSanPham)
                {
                    itemList.items.Add(new Item()
                    {
                        name = item.NamePro,
                        currency = "USD",
                        price = Math.Round(item.Price / 25000, 2).ToString("0.00"), // Assume VND to USD ~25000
                        quantity = item.Number.ToString(),
                        sku = item.ProductID.ToString(),
                    });
                    subtotal += Math.Round(item.Price / 25000, 2) * item.Number;
                }
            }

            var payer = new Payer()
            {
                payment_method = "paypal"
            };
            var redirUrls = new RedirectUrls()
            {
                cancel_url = redirectUrl + "&Cancel=true",
                return_url = redirectUrl
            };
            var details = new Details()
            {
                tax = "0",
                shipping = "0",
                subtotal = subtotal.ToString("0.00")
            };
            var amount = new Amount()
            {
                currency = "USD",
                total = subtotal.ToString("0.00"),
                details = details
            };
            var transactionList = new List<Transaction>();
            var paypalOrderId = DateTime.Now.Ticks;
            transactionList.Add(new Transaction()
            {
                description = $"Invoice #{paypalOrderId}",
                invoice_number = paypalOrderId.ToString(),
                amount = amount,
                item_list = itemList
            });

            this.payment = new Payment()
            {
                intent = "sale",
                payer = payer,
                transactions = transactionList,
                redirect_urls = redirUrls
            };
            // Create a payment using a APIContext  
            return this.payment.Create(apiContext);
        }
        public async Task<ActionResult> momo(int id)
        {
           
            var checkid = db.DONHANGs.Where(s => s.MaDonHang == id).FirstOrDefault();
            var tongtien = checkid.TongTien;
            var paymentService = new PaymentService();
            string orderInfo = $"ma kac hasnh - {id}";
            string redirectUrl = Url.Action("callback", "cart", new { id = id }, Request.Url.Scheme);
            string callbackUrl = Url.Action("PremiumFailure", "truyen", null, Request.Url.Scheme);
            string paymentUrl = await paymentService.CreateMoMoPaymentAsync(tongtien, orderInfo, redirectUrl, callbackUrl);

            if (string.IsNullOrEmpty(paymentUrl))
            {
                throw new Exception("Failed to create MoMo payment URL");
            }
            return Redirect(paymentUrl);
        }
       public ActionResult callback(int id)
       {
            var checkid = db.DONHANGs.Where(s=> s.MaDonHang == id).FirstOrDefault();
            if(checkid == null)
            {
                return HttpNotFound();
            }
            // Add momo validation logic here if needed
            return RedirectToAction("Index","Order");
       }
    }
}