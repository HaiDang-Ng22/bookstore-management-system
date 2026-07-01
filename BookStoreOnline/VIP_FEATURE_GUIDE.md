# Hướng dẫn Chức năng Phân loại Khách VIP

## Tổng quan

Chức năng VIP Customer Classification cho phép bạn phân loại khách hàng thành 2 loại:
- **Regular**: Khách hàng thường (chi tiêu < 5,000,000 VND)
- **VIP**: Khách hàng VIP (chi tiêu >= 5,000,000 VND)

## Cấu trúc Database

### Cột mới được thêm vào bảng KHACHHANG:
- `LoaiKhachHang` (nvarchar(50)): Loại khách ('Regular' hoặc 'VIP')
- `TongChiTieu` (bigint): Tổng tiền khách hàng đã chi tiêu
- `NgayCapNhatVIP` (datetime): Ngày cập nhật lần cuối trạng thái VIP

### Stored Procedures mới:
- `sp_UpdateCustomerType`: Cập nhật loại khách dựa trên tổng chi tiêu
- `sp_GetVIPStatistics`: Lấy thống kê về khách VIP và regular

## Hướng dẫn cài đặt

### 1. Chạy Migration SQL
Chạy tệp SQL tại đường dẫn: `SQL/AddVIPCustomerClassification.sql`

Hoặc chạy các câu lệnh SQL sau:

```sql
-- Chạy file migration
USE [NhaSach]
GO
-- (Nội dung từ AddVIPCustomerClassification.sql)
```

### 2. Update Entity Model
Đã thêm 3 properties mới vào `KHACHHANG.cs`:
```csharp
public string LoaiKhachHang { get; set; }
public Nullable<long> TongChiTieu { get; set; }
public Nullable<System.DateTime> NgayCapNhatVIP { get; set; }
```

### 3. Sử dụng trong Code

#### A. Khởi tạo khách mới
Khi khách đăng ký, chúng ta sẽ tự động gán `LoaiKhachHang = 'Regular'`:

```csharp
var newCustomer = new KHACHHANG
{
    Ten = "Tên khách",
    Email = "email@example.com",
    LoaiKhachHang = "Regular",  // Khách mới luôn là Regular
    TongChiTieu = 0,
    NgayCapNhatVIP = DateTime.Now
};
```

#### B. Cập nhật trạng thái VIP tự động
Sử dụng `CustomerTypeService` để cập nhật trạng thái VIP dựa trên chi tiêu:

```csharp
var customerService = new CustomerTypeService(db);

// Cập nhật trạng thái VIP cho một khách
customerService.UpdateCustomerType(customerId);

// Lấy tổng chi tiêu
long totalSpending = customerService.CalculateTotalSpending(customerId);

// Xác định loại khách dựa trên chi tiêu
string customerType = customerService.DetermineCustomerType(totalSpending);
```

#### C. Lấy thông tin VIP Benefits
```csharp
var customerService = new CustomerTypeService(db);
var vipBenefits = customerService.GetVIPBenefits(customerId);

// Sử dụng thông tin VIP
if (vipBenefits.IsVIP)
{
    decimal discount = (decimal)vipBenefits.DiscountPercentage / 100;
    // Áp dụng 10% discount cho VIP
}
else
{
    long remaining = vipBenefits.RemainingSpendingForVIP;
    // Hiển thị "Cần chi thêm {remaining} VND để thành VIP"
}
```

#### D. Kiểm tra khách gần VIP
```csharp
var customerService = new CustomerTypeService(db);
bool isNearVIP = customerService.IsNearVIPStatus(customerId);

if (isNearVIP)
{
    // Hiển thị "Bạn sắp trở thành VIP! Chi thêm một chút nữa..."
}
```

## API Endpoints

### 1. UserController

#### GET /User/GetVIPInfo
Lấy thông tin VIP của khách hiện tại
```json
{
    "isVIP": true,
    "customerType": "VIP",
    "totalSpending": 5500000,
    "discountPercentage": 10,
    "remainingSpendingForVIP": 0,
    "lastVIPUpdateDate": "2024-01-15T10:30:00"
}
```

#### GET /User/GetProfile
Lấy thông tin hồ sơ với VIP info
- Response: Trang thông tin khách hàng với VIPBenefits

### 2. OrderController

#### POST /Order/UpdateCustomerVIPStatus
Cập nhật trạng thái VIP sau khi đơn hàng xử lý
- Parameters: `orderId` (int)
- Response: `{ success: true/false, message: "..." }`

#### GET /Order/GetCustomerVIPBenefits
Lấy thông tin lợi ích VIP của khách
- Response: JSON VIPBenefits object

## Hiển thị VIP Status trong View

### 1. Hiển thị badge VIP
```html
@if (Model.LoaiKhachHang == "VIP")
{
    <span class="badge badge-gold">VIP Member</span>
}
else
{
    <span class="badge badge-silver">Regular Member</span>
}
```

### 2. Hiển thị tiến trình VIP
```html
@{
    var percentToVIP = Model.TongChiTieu.HasValue 
        ? (int)(Math.Min(Model.TongChiTieu.Value, 5000000) * 100 / 5000000) 
        : 0;
}
<div class="progress">
    <div class="progress-bar" style="width: @percentToVIP%">
        @Model.TongChiTieu?.ToString("#,##0") VND / 5,000,000 VND
    </div>
</div>
```

### 3. Hiển thị lợi ích VIP
```html
@{
    var customerService = new BookStoreOnline.Core.CustomerTypeService();
    var benefits = customerService.GetVIPBenefits(Model.MaKH);
}
<div class="vip-benefits">
    @if (benefits.IsVIP)
    {
        <h4>🎁 Lợi ích VIP của bạn:</h4>
        <ul>
            <li>Giảm giá @benefits.DiscountPercentage% cho tất cả đơn hàng</li>
            <li>Ưu tiên xử lý đơn hàng</li>
            <li>Hỗ trợ khách hàng ưu tiên</li>
            <li>Tham gia chương trình khuyến mại VIP độc quyền</li>
        </ul>
    }
    else
    {
        <h4>🎯 Trở thành VIP:</h4>
        <p>Cần chi thêm <strong>@benefits.RemainingSpendingForVIP.ToString("#,##0") VND</strong> để thành VIP</p>
    }
</div>
```

## Thông tin Thống kê VIP

Để lấy thống kê VIP, sử dụng:

```csharp
var customerService = new CustomerTypeService(db);
var stats = customerService.GetCustomerStatistics();

// Sử dụng
int vipCustomerCount = stats.VIPCustomersCount;
double vipPercentage = stats.VIPPercentage;
long averageSpendingPerVIP = stats.AverageSpendingPerVIP;
```

## Quy trình tự động cập nhật VIP

1. **Đăng ký khách mới**: Tự động set `LoaiKhachHang = 'Regular'`
2. **Đăng nhập**: Kiểm tra lại trạng thái VIP dựa trên chi tiêu hiện tại
3. **Hoàn thành đơn hàng**: Cập nhật `TongChiTieu` và `LoaiKhachHang` nếu cần
4. **Hủy đơn hàng**: Cập nhật lại `TongChiTieu`

## Ngưỡng VIP

Hiện tại, ngưỡng để trở thành VIP là:
- **Chi tiêu từ 5,000,000 VND trở lên → VIP**

Để thay đổi, chỉnh sửa hằng số trong `CustomerTypeService.cs`:
```csharp
private const long VIP_SPENDING_THRESHOLD = 5000000;  // Thay đổi giá trị này
```

## Chiết khấu VIP

VIP customers nhận được:
- **Giảm giá 10%** cho tất cả đơn hàng

Để thay đổi, chỉnh sửa trong `CustomerTypeService.cs`:
```csharp
private const decimal VIP_DISCOUNT_PERCENTAGE = 0.1m;  // Thay đổi giá trị này
```

## Troubleshooting

### 1. Khách hàng chưa cập nhật VIP
- Chạy: `EXEC sp_UpdateCustomerType @MaKH = <customer_id>`
- Hoặc gọi: `customerService.UpdateCustomerType(customerId);`

### 2. Cập nhật tất cả khách hàng
```sql
-- Chạy SQL
DECLARE @MaKH INT
DECLARE customer_cursor CURSOR FOR 
SELECT MaKH FROM KHACHHANG WHERE TrangThai = 1

OPEN customer_cursor
FETCH NEXT FROM customer_cursor INTO @MaKH
WHILE @@FETCH_STATUS = 0
BEGIN
    EXEC sp_UpdateCustomerType @MaKH
    FETCH NEXT FROM customer_cursor INTO @MaKH
END
CLOSE customer_cursor
DEALLOCATE customer_cursor
```

## Lợi ích VIP

| Lợi ích | Regular | VIP |
|---------|---------|-----|
| Giảm giá | 0% | 10% |
| Ưu tiên xử lý đơn | ❌ | ✅ |
| Hỗ trợ ưu tiên | ❌ | ✅ |
| Chương trình độc quyền | ❌ | ✅ |

## Tích hợp thêm (tùy chọn)

Bạn có thể tích hợp thêm:

1. **Email notification**: Gửi email khi khách trở thành VIP
2. **VIP Dashboard**: Trang riêng để khách xem lợi ích VIP
3. **VIP Loyalty Program**: Tích điểm, nhận thưởng
4. **Tiered VIP Levels**: Gold, Platinum, Diamond...
5. **VIP Expiration**: VIP hết hạn sau 1 năm nếu không có hoạt động

## Liên hệ

Nếu có vấn đề, vui lòng liên hệ nhóm phát triển.
