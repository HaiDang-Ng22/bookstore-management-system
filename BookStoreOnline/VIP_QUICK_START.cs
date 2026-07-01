// VIP Customer Classification - Quick Start Guide

// ============================================================
// 1. INITIALIZATION - Setup khi khách đăng ký
// ============================================================

// Trong UserController.SignUp method (ĐANG CÓ):
new_customer.LoaiKhachHang = "Regular";    // Always start as Regular
new_customer.TongChiTieu = 0;
new_customer.NgayCapNhatVIP = DateTime.Now;


// ============================================================
// 2. LOGIN - Cập nhật VIP status khi đăng nhập
// ============================================================

// Trong UserController.Login method (ĐANG CÓ):
var customerService = new CustomerTypeService(db);
customerService.UpdateCustomerType(account.MaKH);


// ============================================================
// 3. CHECK VIP STATUS - Kiểm tra trạng thái VIP
// ============================================================

var customerService = new CustomerTypeService(db);

// 3a. Kiểm tra là VIP không
if (customer.LoaiKhachHang == "VIP")
{
    // Khách là VIP - Áp dụng 10% discount
    decimal discount = 0.1m;
}

// 3b. Kiểm tra gần VIP không
bool isNearVIP = customerService.IsNearVIPStatus(customerId);
if (isNearVIP)
{
    // Hiển thị "Bạn sắp thành VIP" message
}

// 3c. Lấy toàn bộ thông tin VIP
var vipBenefits = customerService.GetVIPBenefits(customerId);
// vipBenefits.IsVIP (bool)
// vipBenefits.CustomerType (string: "VIP" or "Regular")
// vipBenefits.TotalSpending (long)
// vipBenefits.DiscountPercentage (int: 0 or 10)
// vipBenefits.RemainingSpendingForVIP (long)
// vipBenefits.LastVIPUpdateDate (DateTime)


// ============================================================
// 4. GET VIP DISCOUNT - Lấy % giảm giá
// ============================================================

var customerService = new CustomerTypeService(db);
decimal discountPercentage = customerService.GetVIPDiscount(customerId);

// Usage
decimal totalPrice = 1000000;
decimal discount = totalPrice * discountPercentage;
decimal finalPrice = totalPrice - discount;


// ============================================================
// 5. UPDATE VIP STATUS - Cập nhật VIP status
// ============================================================

// Cập nhật sau khi order completed
var customerService = new CustomerTypeService(db);
customerService.UpdateCustomerType(customerId);

// Hoặc sử dụng Stored Procedure
// EXEC sp_UpdateCustomerType @MaKH = 123


// ============================================================
// 6. DISPLAY IN VIEWS - Hiển thị trong Razor views
// ============================================================

// 6a. Simple badge
@if (Model.LoaiKhachHang == "VIP")
{
    <span class="badge badge-gold">VIP</span>
}
else
{
    <span class="badge badge-default">Regular</span>
}

// 6b. Sử dụng partial view (recommended)
@Html.Partial("_VIPStatus", Model)

// 6c. Display progress to VIP
@{
    int percentage = (int)((double)Model.TongChiTieu ?? 0 / 5000000 * 100);
}
<div class="progress">
    <div class="progress-bar" style="width: @percentage%">
        @Model.TongChiTieu.ToString("#,##0") / 5,000,000 VND
    </div>
</div>


// ============================================================
// 7. API ENDPOINTS - Gọi từ JavaScript/Frontend
// ============================================================

// 7a. Get VIP Info
fetch('/User/GetVIPInfo')
    .then(res => res.json())
    .then(data => {
        if (data.isVIP) {
            console.log('VIP discount: ' + data.discountPercentage + '%');
        } else {
            console.log('Need: ' + data.remainingSpendingForVIP + ' VND to VIP');
        }
    });

// 7b. Get Customer VIP Benefits
fetch('/Order/GetCustomerVIPBenefits')
    .then(res => res.json())
    .then(data => displayVIPStatus(data));

// 7c. Update Customer VIP Status after order
fetch('/Order/UpdateCustomerVIPStatus', {
    method: 'POST',
    body: JSON.stringify({ orderId: 123 }),
    headers: { 'Content-Type': 'application/json' }
})
.then(res => res.json())
.then(data => console.log(data));


// ============================================================
// 8. GET STATISTICS - Lấy thống kê VIP
// ============================================================

var customerService = new CustomerTypeService(db);
var stats = customerService.GetCustomerStatistics();

// stats.TotalCustomers (int)
// stats.VIPCustomersCount (int)
// stats.RegularCustomersCount (int)
// stats.VIPPercentage (double)
// stats.TotalSpendingAll (long)
// stats.TotalSpendingVIP (long)
// stats.AverageSpendingPerCustomer (long)
// stats.AverageSpendingPerVIP (long)

// Usage
Console.WriteLine($"VIP Customers: {stats.VIPCustomersCount}/{stats.TotalCustomers} ({stats.VIPPercentage:F1}%)");
Console.WriteLine($"Average VIP spending: {stats.AverageSpendingPerVIP:N0} VND");


// ============================================================
// 9. GET TOP VIP CUSTOMERS - Lấy top VIP customers
// ============================================================

var customerService = new CustomerTypeService(db);
var topVIPs = customerService.GetTopVIPCustomers(count: 10).ToList();

foreach (var vip in topVIPs)
{
    Console.WriteLine($"{vip.Ten}: {vip.TongChiTieu:N0} VND");
}


// ============================================================
// 10. APPLY DISCOUNT - Áp dụng discount VIP
// ============================================================

public decimal CalculateOrderTotal(int customerId, List<OrderItem> items)
{
    var customerService = new CustomerTypeService(db);
    decimal subtotal = items.Sum(i => i.Price * i.Quantity);
    
    decimal discount = customerService.GetVIPDiscount(customerId);
    decimal discountAmount = subtotal * discount;
    decimal total = subtotal - discountAmount;
    
    return total;
}


// ============================================================
// 11. UPDATE AFTER ORDER COMPLETION - Cập nhật sau khi xử lý đơn
// ============================================================

// Trong OrderController hoặc Admin area
public void CompleteOrder(int orderId)
{
    var order = db.DONHANGs.Find(orderId);
    order.TrangThai = (int)Constants.StatusOrder.Completed;
    db.SaveChanges();
    
    // Update customer VIP status
    if (order.ID.HasValue)
    {
        var customerService = new CustomerTypeService(db);
        customerService.UpdateCustomerType(order.ID.Value);
    }
}


// ============================================================
// 12. BATCH UPDATE ALL CUSTOMERS - Cập nhật tất cả khách
// ============================================================

public void UpdateAllCustomersVIPStatus()
{
    var db = new NhaSachEntities3();
    var customerService = new CustomerTypeService(db);
    
    var allCustomers = db.KHACHHANGs
        .Where(k => k.TrangThai == true)
        .ToList();
    
    foreach (var customer in allCustomers)
    {
        customerService.UpdateCustomerType(customer.MaKH);
        System.Threading.Thread.Sleep(100); // Avoid overload
    }
    
    Console.WriteLine($"Updated {allCustomers.Count} customers");
}


// ============================================================
// 13. SEND VIP NOTIFICATION - Gửi thông báo khi trở thành VIP
// ============================================================

public void CheckAndNotifyNewVIP()
{
    var db = new NhaSachEntities3();
    var customerService = new CustomerTypeService(db);
    
    // Get all customers updated in last 24 hours
    var recentlyUpdated = db.KHACHHANGs
        .Where(k => k.NgayCapNhatVIP >= DateTime.Now.AddDays(-1)
             && k.LoaiKhachHang == "VIP")
        .ToList();
    
    foreach (var customer in recentlyUpdated)
    {
        // Check if just became VIP
        var spending = customerService.CalculateTotalSpending(customer.MaKH);
        if (spending >= 5000000)
        {
            SendVIPWelcomeEmail(customer.Email, customer.Ten);
        }
    }
}


// ============================================================
// 14. CONFIGURATION - Thay đổi cấu hình
// ============================================================

// Để thay đổi ngưỡng VIP, sửa trong CustomerTypeService.cs:
private const long VIP_SPENDING_THRESHOLD = 5000000;  // Thay đổi giá trị này

// Để thay đổi % discount VIP, sửa trong CustomerTypeService.cs:
private const decimal VIP_DISCOUNT_PERCENTAGE = 0.1m;  // 0.1 = 10%, 0.15 = 15%, etc.


// ============================================================
// 15. SQL QUERIES - Câu SQL hữu ích
// ============================================================

-- Get all VIP customers
SELECT * FROM KHACHHANG WHERE LoaiKhachHang = 'VIP' ORDER BY TongChiTieu DESC;

-- Get VIP statistics
EXEC sp_GetVIPStatistics;

-- Update specific customer VIP
EXEC sp_UpdateCustomerType @MaKH = 123;

-- Find customers near VIP (within 1M VND)
SELECT * FROM KHACHHANG 
WHERE LoaiKhachHang = 'Regular' 
  AND TongChiTieu >= 4000000 AND TongChiTieu < 5000000
ORDER BY TongChiTieu DESC;

-- Get top 10 spenders
SELECT TOP 10 MaKH, Ten, LoaiKhachHang, TongChiTieu 
FROM KHACHHANG 
WHERE TrangThai = 1 
ORDER BY TongChiTieu DESC;

-- Reset all VIP status (not recommended!)
-- UPDATE KHACHHANG SET LoaiKhachHang = 'Regular', TongChiTieu = 0;


// ============================================================
// 16. TESTING - Unit test examples
// ============================================================

[Test]
public void TestVIPCalculation()
{
    var service = new CustomerTypeService();
    
    // Test 1: Regular customer
    var type1 = service.DetermineCustomerType(3000000);
    Assert.AreEqual("Regular", type1);
    
    // Test 2: VIP customer
    var type2 = service.DetermineCustomerType(5000000);
    Assert.AreEqual("VIP", type2);
    
    // Test 3: VIP discount
    var discount = service.GetVIPDiscount(vipCustomerId);
    Assert.AreEqual(0.1m, discount);
}


// ============================================================
// 17. LOGGING & MONITORING
// ============================================================

public class VIPLogger
{
    private static readonly log4net.ILog log = log4net.LogManager.GetLogger(typeof(VIPLogger));
    
    public static void LogVIPChange(int customerId, string oldType, string newType)
    {
        log.Info($"Customer {customerId} changed from {oldType} to {newType}");
    }
    
    public static void LogVIPSpending(int customerId, long newSpending)
    {
        log.Info($"Customer {customerId} spending updated to {newSpending:N0}");
    }
}


// ============================================================
// 18. ERROR HANDLING - Xử lý lỗi
// ============================================================

public VIPBenefits GetVIPBenefitsWithErrorHandling(int customerId)
{
    try
    {
        var service = new CustomerTypeService(db);
        var benefits = service.GetVIPBenefits(customerId);
        
        if (benefits == null)
        {
            log.Warn($"VIP benefits is null for customer {customerId}");
            return new VIPBenefits 
            { 
                IsVIP = false, 
                CustomerType = "Regular", 
                TotalSpending = 0,
                DiscountPercentage = 0,
                RemainingSpendingForVIP = 5000000
            };
        }
        
        return benefits;
    }
    catch (Exception ex)
    {
        log.Error($"Error getting VIP benefits for customer {customerId}: {ex}");
        return null;
    }
}


// ============================================================
// SUMMARY - Tóm tắt
// ============================================================

/*
 * VIP Classification System:
 * 
 * Regular: <5,000,000 VND
 * VIP: >=5,000,000 VND (10% discount)
 * 
 * Automatic updates on:
 * - New registration (Regular)
 * - Login (recalculate)
 * - Order completion (recalculate)
 * - Order cancellation (recalculate)
 * 
 * Key Classes:
 * - CustomerTypeService: Main business logic
 * - VIPBenefits: DTO for VIP info
 * - CustomerStatistics: DTO for stats
 * 
 * Key Methods:
 * - UpdateCustomerType(customerId)
 * - GetVIPBenefits(customerId)
 * - GetVIPDiscount(customerId)
 * - IsNearVIPStatus(customerId)
 * - GetCustomerStatistics()
 * 
 * Database:
 * - 3 new columns in KHACHHANG
 * - 2 stored procedures
 * - 1 index for performance
*/
