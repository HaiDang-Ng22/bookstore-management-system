using System; 
using System.IdentityModel.Tokens.Jwt; 
using System.Linq; 
using System.Security.Claims; 
using System.Text; 
using System.Web.Mvc; 
using Microsoft.IdentityModel.Tokens;
using BookStoreOnline.Models;
using System.Security.Cryptography;
using System.Web;
using System.Net.Mail;
using System.Net;

public class UserController : Controller
{
    private NhaSachEntities3 db = new NhaSachEntities3();

    private const string SecretKey = "your_new_very_long_secret_key_at_least_32_characters!"; 

    private string HashPassword(string password) 
    {
        using (var sha256 = SHA256.Create()) 
        { 
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password)); 
            return Convert.ToBase64String(bytes); 
        }
    }

    private string GenerateAccessToken(KHACHHANG user) 
    {
        var tokenHandler = new JwtSecurityTokenHandler(); 
        var key = Encoding.UTF8.GetBytes(SecretKey); 
 
        var tokenDescriptor = new SecurityTokenDescriptor 
        {
            Subject = new ClaimsIdentity(new[] 
            {
                new Claim(ClaimTypes.Name, user.Ten ?? ""), 
                new Claim(ClaimTypes.Email, user.Email ?? ""), 
                new Claim("TrangThai", user.TrangThai.ToString()), 
                new Claim("NgayTao", user.NgayTao?.ToString("yyyy-MM-dd HH:mm:ss") ?? "") 
 
            }),
            Expires = DateTime.UtcNow.AddHours(1), // set access_token for 1 hour 
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature) 
        };

        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    private string GenerateRefreshToken()
    {
        return Convert.ToBase64String(Guid.NewGuid().ToByteArray());
    }
    private void SetAccessTokenCookie(string accessToken)
    {

        var cookie = new HttpCookie("accessToken", accessToken)
        {
            HttpOnly = true,
            Secure = Request.IsSecureConnection,
            Expires = DateTime.UtcNow.AddHours(1),
            SameSite = SameSiteMode.Strict
        };
        Response.Cookies.Add(cookie);
    }
    private void SetRefreshTokenCookie(string refreshToken)
    {
        var cookie = new HttpCookie("refreshToken", refreshToken)
        {
            HttpOnly = true, // Bảo mật: Cookie chỉ có thể truy cập từ server
            Secure = Request.IsSecureConnection, // Chỉ gửi cookie qua HTTPS nếu có
            Expires = DateTime.UtcNow.AddDays(7), // Thời hạn của refresh token
            SameSite = SameSiteMode.Strict
        };
        Response.Cookies.Add(cookie);
    }

    private string GetRefreshTokenFromCookie()
    {
        var cookie = Request.Cookies["refreshToken"];
        return cookie != null ? cookie.Value : null;
    }

    private void RemoveRefreshTokenCookie()
    {
        if (Request.Cookies["refreshToken"] != null)
        {
            var cookie = new HttpCookie("refreshToken")
            {
                Expires = DateTime.UtcNow.AddDays(-1)
            };
            Response.Cookies.Add(cookie);
        }
    }

    [HttpGet]
    public ActionResult SignUp()
    {
        return View();
    }

    [HttpPost]
    public ActionResult SignUp(KHACHHANG cus, string rePass)
    {
        if (!ModelState.IsValid)
        {
            var errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage).ToList();
            var exceptionErrors = ModelState.Values.SelectMany(v => v.Errors).Where(e => e.Exception != null).Select(e => e.Exception.Message).ToList();
            errors.AddRange(exceptionErrors);
            ViewBag.ThongBao = "Lỗi đăng ký (Dữ liệu không hợp lệ): " + string.Join("; ", errors);
            return View();
        }

        try
        {
            var checkEmail = db.KHACHHANGs.FirstOrDefault(c => c.Email == cus.Email);
            if (checkEmail != null)
            {
                ViewBag.ThongBaoEmail = "Đã có tài khoản đăng nhập bằng Email này";
                return View();
            }
            if (cus.MatKhau.Length < 6)
            {
                ViewBag.ThongBaoMK = "Mật khẩu phải có ít nhất 6 ký tự";
                return View();
            }

            if (cus.MatKhau == rePass)
            {
                cus.TrangThai = true;
                cus.NgayTao = DateTime.Now;
                cus.MatKhau = HashPassword(cus.MatKhau);

                db.KHACHHANGs.Add(cus);
                db.SaveChanges();

                // Tạo token
                var accessToken = GenerateAccessToken(cus);
                SetAccessTokenCookie(accessToken);
                HttpContext.Response.Headers.Add("access_token", accessToken);

                var refreshToken = GenerateRefreshToken();

                // Lưu cả access_token và refresh_token vào cơ sở dữ liệu
                cus.RefreshToken = refreshToken;
                cus.AccessToken = accessToken;
                cus.TokenExpiration = DateTime.UtcNow.AddDays(7); // set refresh_token for 7 days
                db.SaveChanges();

                // Lưu access token vào cookie
                SetAccessTokenCookie(accessToken);

                return RedirectToAction("Index", "Home");
            }
            else
            {
                ViewBag.ThongBao = "Mật khẩu xác nhận không chính xác";
            }
        }
        catch (System.Data.Entity.Validation.DbEntityValidationException ex)
        {
            var errorMessages = ex.EntityValidationErrors
                    .SelectMany(x => x.ValidationErrors)
                    .Select(x => x.ErrorMessage);
            var fullErrorMessage = string.Join("; ", errorMessages);
            ViewBag.ThongBao = "Lỗi xác thực cơ sở dữ liệu: " + fullErrorMessage;
        }
        catch (Exception ex)
        {
            ViewBag.ThongBao = "Lỗi đăng ký hệ thống: " + ex.Message + (ex.InnerException != null ? " -> " + ex.InnerException.Message : "");
        }
        return View();
    }


    [HttpGet]
    public ActionResult Login()
    {
        return View();
    }
    [HttpPost]
    public ActionResult Login(string email, string matkhau)
    {
        if (string.IsNullOrEmpty(email) || string.IsNullOrEmpty(matkhau))
        {
            ViewBag.ThongBao = "Vui lòng nhập đầy đủ email và mật khẩu";
            return View();
        }

        var taikhoanAdmin = db.NHANVIENs.FirstOrDefault(k => k.Email == email && k.MatKhau == matkhau);
        if (taikhoanAdmin != null)
        {
            // Nếu TrangThai bị null, coi như tài khoản chưa kích hoạt hoặc bị khóa (false)
            if (!(taikhoanAdmin.TrangThai ?? false))
            {
                ViewBag.ThongBao = "Tài khoản đã bị khóa";
                return View();
            }

            Session["TaiKhoan"] = taikhoanAdmin;
            return RedirectToAction("Index", "Home_Page", new { area = "Admin" });
        }

        var hashedPassword = HashPassword(matkhau);
        var account = db.KHACHHANGs.FirstOrDefault(k => k.Email == email && k.MatKhau == hashedPassword);

        if (account != null)
        {
            // Hoặc bạn cũng có thể dùng GetValueOrDefault() tương đương với toán tử ??
            if (!account.TrangThai.GetValueOrDefault())
            {
                ViewBag.ThongBao = "Tài khoản đã bị khóa";
                return View();
            }

            // Tạo token mới
            var accessToken = GenerateAccessToken(account);
            var refreshToken = GenerateRefreshToken();

            // Lưu access_token và refresh_token vào cơ sở dữ liệu
            account.AccessToken = accessToken;
            account.RefreshToken = refreshToken;
            account.TokenExpiration = DateTime.UtcNow.AddDays(7);
            db.SaveChanges();

            // Lưu access token vào cookie
            SetAccessTokenCookie(accessToken);

            // Lưu refresh token vào cookie
            SetRefreshTokenCookie(refreshToken);

            Session["TaiKhoan"] = account;
            
            // Sync cart from database
            BookStoreOnline.Controllers.CartController.SyncCartOnLogin(account.MaKH, HttpContext);

            return RedirectToAction("Index", "Home");
        }
        else
        {
            ViewBag.ThongBao = "Email hoặc mật khẩu không chính xác";
        }
        return View();
    }
    private string GetAccessTokenFromCookie()
    {
        var cookie = Request.Cookies["accessToken"];
        return cookie != null ? cookie.Value : null;
    }
    [HttpPost]
    public ActionResult RefreshToken()
    {
        var refreshToken = GetRefreshTokenFromCookie();

        if (string.IsNullOrEmpty(refreshToken))
        {
            return new HttpStatusCodeResult(401, "Refresh Token không hợp lệ hoặc đã hết hạn");
        }

        var user = db.KHACHHANGs.FirstOrDefault(u => u.RefreshToken == refreshToken && u.TokenExpiration > DateTime.UtcNow);

        if (user == null)
        {
            return new HttpStatusCodeResult(401, "Refresh Token không hợp lệ hoặc đã hết hạn");
        }

        // Tạo access token mới
        var newAccessToken = GenerateAccessToken(user);
        user.AccessToken = newAccessToken;
        db.SaveChanges();

        return Json(new { accessToken = newAccessToken });
    }
    [HttpGet]
    public ActionResult ForgotPassword()
    {
        return View();
    }

    [HttpPost]
    public ActionResult ForgotPassword(string email)
    {
        if (string.IsNullOrEmpty(email))
        {
            ViewBag.ThongBao = "Vui lòng nhập email của bạn.";
            return View();
        }

        // Kiểm tra email có tồn tại không
        var user = db.KHACHHANGs.FirstOrDefault(u => u.Email == email);
        if (user == null)
        {
            ViewBag.ThongBao = "Không tìm thấy tài khoản với email này.";
            return View();
        }

        // Tạo token khôi phục mật khẩu
        user.ResetPasswordToken = Guid.NewGuid().ToString();
        user.ResetTokenExpiration = DateTime.UtcNow.AddHours(1); // Token hết hạn sau 1 giờ
        db.SaveChanges();

        // Tạo link khôi phục mật khẩu
        var resetLink = Url.Action("ResetPassword", "User",
                        new { token = user.ResetPasswordToken },
                        Request.Url.Scheme);

        // Gửi email khôi phục mật khẩu
        SendPasswordResetEmail(user.Email, resetLink);

        ViewBag.ThongBao = "Đã gửi email khôi phục mật khẩu. Vui lòng kiểm tra hộp thư của bạn.";
        return View();
    }

    public void SendPasswordResetEmail(string email, string resetLink)
    {
        try
        {
            // Tạo đối tượng MailMessage
            MailMessage mail = new MailMessage();
            mail.From = new MailAddress("manhhoang8th4@gmail.com");
            mail.To.Add(email);
            mail.Subject = "Khôi phục mật khẩu";
            mail.Body = $"Nhấp vào liên kết để đặt lại mật khẩu của bạn: <a href='{resetLink}'>Đặt lại mật khẩu</a>";
            mail.IsBodyHtml = true;

            // Cấu hình SMTP client
            var smtpClient = new SmtpClient("smtp.gmail.com", 587)
            {
                Credentials = new NetworkCredential(Environment.GetEnvironmentVariable("SMTP_USER"), Environment.GetEnvironmentVariable("SMTP_PASS")),
                EnableSsl = true
            };
            smtpClient.Credentials = new NetworkCredential("manhhoang8th4@gmail.com", "jsex khqd lexg wzsq");
            smtpClient.EnableSsl = true; // Kết nối bảo mật

            // Gửi email
            smtpClient.Send(mail);
        }
        catch (SmtpException ex)
        {
            // Xử lý lỗi nếu có
            throw new Exception("Gửi email thất bại. Vui lòng kiểm tra lại cấu hình SMTP.", ex);
        }
    }
    [HttpGet]
    public ActionResult ResetPassword(string token)
    {
        if (string.IsNullOrEmpty(token))
        {
            ViewBag.ThongBao = "Token không hợp lệ.";
            return RedirectToAction("Index","Home");
        }

        var user = db.KHACHHANGs
                    .FirstOrDefault(u => u.ResetPasswordToken == token && u.ResetTokenExpiration > DateTime.UtcNow);

        if (user == null)
        {
            ViewBag.ThongBao = "Token không hợp lệ hoặc đã hết hạn.";
            return RedirectToAction("Login");
        }

        ViewBag.Token = token;
        return View();
    }

    // POST: User/ResetPassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult ResetPassword(string token, string newPassword, string confirmPassword)
    {
        if (string.IsNullOrEmpty(token))
        {
            ViewBag.ThongBao = "Token không hợp lệ.";
            return RedirectToAction("Login");
        }

        if (string.IsNullOrEmpty(newPassword) || string.IsNullOrEmpty(confirmPassword))
        {
            ViewBag.ThongBao = "Vui lòng nhập mật khẩu mới.";
            ViewBag.Token = token;
            return View();
        }

        if (newPassword != confirmPassword)
        {
            ViewBag.ThongBao = "Mật khẩu xác nhận không khớp.";
            ViewBag.Token = token;
            return View();
        }

        // Kiểm tra token trong cơ sở dữ liệu
        var user = db.KHACHHANGs
                    .FirstOrDefault(u => u.ResetPasswordToken == token && u.ResetTokenExpiration > DateTime.UtcNow);

        if (user == null)
        {
            ViewBag.ThongBao = "Token không hợp lệ hoặc đã hết hạn.";
            return RedirectToAction("Login");
        }

        try
        {
            // Mã hóa mật khẩu mới
            user.MatKhau = HashPassword(newPassword);
            user.ResetPasswordToken = null;  // Xóa token sau khi sử dụng
            user.ResetTokenExpiration = null;  // Xóa thời gian hết hạn token
            db.SaveChanges();  // Lưu thay đổi vào cơ sở dữ liệu

            TempData["ThongBao"] = "Mật khẩu đã được đặt lại thành công!";
            return RedirectToAction("Login");
        }
        catch (Exception ex)
        {
            ViewBag.ThongBao = "Đã xảy ra lỗi khi đặt lại mật khẩu. Vui lòng thử lại.";
            ViewBag.Token = token;
            return View();
        }
    }

    // Phương thức mã hóa mật khẩu (giả sử bạn đã cài sẵn phương thức này)
  
    [HttpGet]
    public ActionResult ChangePassword()
    {
        return View();
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult ChangePassword(string oldPassword, string newPassword, string confirmPassword)
    {
        if (ModelState.IsValid)
        {
            var currentUser = Session["TaiKhoan"] as KHACHHANG;

            if (currentUser != null)
            {
                var user = db.KHACHHANGs.FirstOrDefault(u => u.MaKH == currentUser.MaKH);

                if (user != null)
                {
                    if (user.MatKhau == HashPassword(oldPassword))
                    {
                        if (newPassword == confirmPassword)
                        {
                            user.MatKhau = HashPassword(newPassword);
                            db.SaveChanges();
                            ViewBag.ThongBao = "Mật khẩu đã được thay đổi thành công!";
                        }
                        else
                        {
                            ViewBag.ThongBao = "Mật khẩu xác nhận không chính xác";
                        }
                    }
                    else
                    {
                        ViewBag.ThongBao = "Mật khẩu cũ không chính xác";
                    }
                }
                else
                {
                    ViewBag.ThongBao = "Người dùng không tồn tại";
                }
            }
            else
            {
                ViewBag.ThongBao = "Người dùng chưa đăng nhập";
            }
        }
        return View();
    }
    [HttpGet]
    public ActionResult UpdateInfo()
    {
        var currentUser = Session["TaiKhoan"] as KHACHHANG;

        if (currentUser != null)
        {
            var user = db.KHACHHANGs.FirstOrDefault(u => u.MaKH == currentUser.MaKH);
            if (user != null)
            {
                return View(user);
            }
        }
        return RedirectToAction("Login", "User");
    }
    [HttpPost]
    [ValidateAntiForgeryToken]
    public ActionResult UpdateInfo(KHACHHANG model)
    {
        var currentUser = Session["TaiKhoan"] as KHACHHANG;

        if (currentUser != null)
        {
            var user = db.KHACHHANGs.FirstOrDefault(u => u.MaKH == currentUser.MaKH);
            if (user != null)
            {
                // Kiểm tra nếu email đã tồn tại trong database
                if (db.KHACHHANGs.Any(u => u.Email == model.Email && u.MaKH != user.MaKH))
                {
                    // Nếu email đã tồn tại (trùng email với một tài khoản khác), hiển thị lỗi
                    ModelState.AddModelError("Email", "Email này đã tồn tại trong hệ thống.");
                    return View(user);  // Trả về View với dữ liệu và thông báo lỗi
                }

                // Cập nhật thông tin người dùng nếu không có lỗi
                user.Ten = model.Ten;
                user.Email = model.Email;
                user.DiaChi = model.DiaChi;
                user.SoDienThoai = model.SoDienThoai;

                db.SaveChanges();
                ViewBag.ThongBao = "Thông tin đã được cập nhật thành công.";
                return View(user);  // Trả về View với thông báo thành công
            }
        }

        return RedirectToAction("Login", "User");
    }

    public ActionResult LogOut()
    {
        // Clear session data (if any)
        Session.Clear();
        Session.Abandon();

        if (Request.Cookies["accessToken"] != null)
        {
            var accessTokenCookie = new HttpCookie("accessToken")
            {
                Expires = DateTime.UtcNow.AddDays(-1) // Set the cookie's expiration to a past date
            };
            Response.Cookies.Add(accessTokenCookie);
        }

        if (Request.Cookies["refreshToken"] != null)
        {
            var refreshTokenCookie = new HttpCookie("refreshToken")
            {
                Expires = DateTime.UtcNow.AddDays(-1) // Set the cookie's expiration to a past date
            };
            Response.Cookies.Add(refreshTokenCookie);
        }

        // Optionally, you can redirect the user to the homepage or login page after logout
        return RedirectToAction("Login", "User");
    }
} 
