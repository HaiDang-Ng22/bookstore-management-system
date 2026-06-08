# BookStoreOnline - Dự Án Nhà Sách Trực Tuyến

Dự án thực hành môn học Thương mại điện tử: Xây dựng website bán sách trực tuyến sử dụng ASP.NET MVC 5 và Entity Framework.

---

## 🔗 Tài liệu liên quan
* **Bảng tiến độ & Product Backlog:** [Google Sheets](https://docs.google.com/spreadsheets/d/1IAEy-JfDoP9tZ6jsPZFjDkG-6RMTCyMpGRs5qwoNu7E/edit?hl=vi&gid=2070379104#gid=2070379104)
* **Kịch bản kiểm thử (Test Cases):** [Google Docs](https://docs.google.com/document/d/12VwvqE7veXStKUt0VwFAsbPb8rjHgPoH/edit?usp=sharing&ouid=112040097888957867090&rtpof=true&sd=true)
* **Link Website Demo:** [BookStoreOnline](http://manhhoang8t4-001-site1.ltempurl.com/)

---

## 🛠️ Công nghệ sử dụng
- **Backend:** C# (.NET Framework 4.8), ASP.NET MVC 5.
- **ORM:** Entity Framework 6 (Database First).
- **Frontend:** HTML5, CSS3, Javascript, Bootstrap.
- **Xác thực:** Session kết hợp JWT (JSON Web Tokens) lưu trong HttpOnly Cookies.
- **Cơ sở dữ liệu:** Microsoft SQL Server.
- **Thanh toán:** Tích hợp cổng thanh toán PayPal, VNPay.

---

## ✨ Các tính năng chính

### 🛒 Dành cho Khách hàng
1. **Duyệt sách:** Xem danh sách sách mới, bán chạy, lọc theo danh mục hoặc tìm kiếm theo từ khóa.
2. **Chi tiết sản phẩm:** Xem thông tin chi tiết, tác giả, giá bán, mô tả sách và đánh giá của người mua khác.
3. **Giỏ hàng:** Thêm sản phẩm, điều chỉnh số lượng và cập nhật giỏ hàng trực quan.
4. **Đặt hàng & Thanh toán:** Hỗ trợ thanh toán khi nhận hàng (COD), cổng thanh toán PayPal.
5. **Quản lý tài khoản:** Đăng ký, đăng nhập, đổi mật khẩu, quên mật khẩu (gửi email đặt lại mật khẩu qua SMTP Google) và xem lịch sử đơn hàng.

### 🛡️ Dành cho Quản trị viên (Admin Area)
1. **Bảng điều khiển (Dashboard):** Xem tổng quan số lượng sách, khách hàng, đơn hàng.
2. **Quản lý danh mục:** Thêm, sửa, xóa các thể loại sách.
3. **Quản lý sản phẩm:** Thêm sách mới, tải lên hình ảnh sách, cập nhật giá bán, số lượng tồn kho.
4. **Quản lý đơn hàng:** Duyệt đơn hàng, cập nhật trạng thái đơn hàng (Đã xác nhận, Đang giao, Đã giao, Hủy đơn).
5. **Quản lý người dùng & phân quyền:** Phân cấp vai trò nhân viên (Administrator, Manager, Seller) và kích hoạt/khóa tài khoản nhân viên hoặc khách hàng.

---

## 🔑 Tài khoản thử nghiệm (Demo Accounts)

### 1. Tài khoản Quản trị (Admin)
- **Đường dẫn:** `/Admin/Home_Page`
- **Email:** `admin@gmail.com`
- **Mật khẩu:** `123456`
- *(Hoặc tài khoản phụ: `admin@bookstore.com` / `admin`)*

### 2. Tài khoản Khách hàng (Customer)
- **Email:** `customer@bookstore.com`
- **Mật khẩu:** `123456`

---

## 💻 Hướng dẫn chạy dự án dưới Local

1. **Yêu cầu hệ thống:**
   - Cài đặt Visual Studio 2022 (chọn gói *ASP.NET and web development*).
   - SQL Server (hoặc SQL LocalDB).

2. **Các bước thiết lập:**
   - Clone repository này về máy.
   - Mở file solution `BookStoreOnline.sln` bằng Visual Studio.
   - Nhấp chuột phải vào Solution -> Chọn **Restore NuGet Packages** để tự động tải các thư viện cần thiết.
   - Chạy các file SQL trong thư mục `/SQL` trên SQL Server của bạn nếu muốn tạo database local riêng, hoặc giữ nguyên chuỗi kết nối trong `Web.config` để chạy trực tiếp trên database cloud.
   - Nhấn **F5** để khởi chạy website. Cơ chế Seeder tự động sẽ gieo dữ liệu mẫu ngay khi dự án được chạy lần đầu nếu database trống.
