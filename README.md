# BookStoreOnline - Dự Án Nhà Sách Trực Tuyến

Dự án thực hành môn học Thương mại điện tử: Xây dựng website bán sách trực tuyến sử dụng ASP.NET MVC 5 và Entity Framework.
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

### 📝 Nhật ký cập nhật tiến độ công việc - Khải Tấn

#### 1. Phần Thanh toán / Đăng ký
- **Gọi API địa chỉ (Đơn vị hành chính):** Tích hợp thành công bộ 3 ô chọn (Tỉnh/Thành phố, Quận/Huyện, Xã/Phường) và ô nhập Số nhà tại form điền thông tin đơn hàng. 
- Sử dụng JavaScript để gọi API công cộng, tự động đổ dữ liệu động khi chọn Tỉnh -> Huyện -> Xã.
- Xử lý gộp dữ liệu thành một chuỗi địa chỉ hoàn chỉnh (`Số nhà, Xã, Huyện, Tỉnh`) tự động điền vào ô input ẩn để lưu trữ đồng bộ xuống Cơ sở dữ liệu khi khách hàng tiến hành đặt hàng hoặc đăng ký thông tin nhận hàng.

#### 2. Fix lỗi hệ thống
- **Sửa lỗi Compilation Error (Details.cshtml):** Khắc phục lỗi toán tử `?` (`Operator '?' cannot be applied to operand of type 'int'`) tại dòng hiển thị `@Model.TongTien` do thuộc tính trong Database không cho phép rỗng.
- **Sửa lỗi hiển thị tên sản phẩm (Details.cshtml):** Khắc phục lỗi giao diện hiển thị chuỗi proxy động dài ngoằng của Entity Framework (`System.Data.Entity.DynamicProxies.SANPHAM_...`). Đã ép lại cú pháp hiển thị chuẩn để bóc tách đúng chuỗi Text tên sách tiếng Việt.
- **Sửa lỗi/Cập nhật hiển thị số lượng sách (Index.cshtml ngoài Trang chủ):** Thay đổi logic tính toán số lượng kho hiển thị tăng thêm 100 cuốn dựa trên số lượng gốc của Database theo yêu cầu, đồng thời tối ưu lại các hàm điều kiện `@if` check hiển thị tag "Hết hàng".
- **Cập nhật hiển thị Trạng thái thanh toán (Index.cshtml của Order):** Sửa lỗi trang lịch sử đơn hàng hiển thị mặc định một chữ "Chưa thanh toán", chuyển sang cấu trúc check điều kiện để hiển thị rõ ràng bằng màu sắc: Đã thanh toán Online, Thanh toán COD (Tiền mặt), Chờ thanh toán Online.

#### 3. Phần Admin - Danh mục sách
- Rà soát hệ thống quản lý danh mục sách trong phân hệ Admin.
- Đảm bảo các chức năng cốt lõi (Xem danh sách danh mục, Thêm mới, Sửa thông tin và Xóa danh mục sách) liên kết dữ liệu chuẩn xác, không bị lỗi khi đồng bộ danh mục phân loại ra ngoài giao diện Trang chủ.
