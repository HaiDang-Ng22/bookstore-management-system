using System;
using System.Linq;
using BookStoreOnline.Models;
using static BookStoreOnline.Areas.Admin.Constants.Constants;

namespace BookStoreOnline.Core
{
    /// <summary>
    /// Service to manage customer classification (VIP or Regular)
    /// </summary>
    public class CustomerTypeService
    {
        private readonly NhaSachEntities3 db;

        // VIP thresholds
        private const long VIP_SPENDING_THRESHOLD = 5000000;  // 5 million VND
        private const decimal VIP_DISCOUNT_PERCENTAGE = 0.1m;  // 10% discount for VIP

        public CustomerTypeService(NhaSachEntities3 context = null)
        {
            db = context ?? new NhaSachEntities3();
        }

        /// <summary>
        /// Calculate total spending for a customer
        /// </summary>
        public long CalculateTotalSpending(int customerId)
        {
            try
            {
                var totalSpending = db.DONHANGs
                    .Where(o => o.ID == customerId &&
                           o.TrangThai == (int)StatusOrder.Received &&
                           o.TrangThaiThanhToan == (int)StatusPayment.Paid)
                    .Sum(o => (long?)o.TongTien) ?? 0;

                return totalSpending;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Determine customer type based on spending
        /// </summary>
        public string DetermineCustomerType(long totalSpending)
        {
            return totalSpending >= VIP_SPENDING_THRESHOLD ? "VIP" : "Regular";
        }

        /// <summary>
        /// Update customer type based on total spending
        /// </summary>
        public bool UpdateCustomerType(int customerId)
        {
            try
            {
                var customer = db.KHACHHANGs.FirstOrDefault(k => k.MaKH == customerId);
                if (customer == null)
                    return false;

                long totalSpending = CalculateTotalSpending(customerId);
                string newType = DetermineCustomerType(totalSpending);

                customer.TongChiTieu = totalSpending;
                customer.LoaiKhachHang = newType;
                customer.NgayCapNhatVIP = DateTime.Now;

                db.SaveChanges();
                return true;
            }
            catch
            {
                return false;
            }
        }

        /// <summary>
        /// Recalculate total spending for all customers based on completed orders.
        /// </summary>
        public int RecalculateAllCustomerSpendings()
        {
            try
            {
                int updatedCount = 0;
                var customers = db.KHACHHANGs.ToList();

                foreach (var customer in customers)
                {
                    if (customer.MaKH <= 0)
                        continue;

                    long totalSpending = db.DONHANGs
                        .Where(o => o.ID == customer.MaKH &&
                               o.TrangThai == (int)StatusOrder.Received &&
                               o.TrangThaiThanhToan == (int)StatusPayment.Paid)
                        .Sum(o => (long?)o.TongTien) ?? 0;

                    string newType = DetermineCustomerType(totalSpending);

                    if (customer.TongChiTieu != totalSpending ||
                        !string.Equals(customer.LoaiKhachHang, newType, StringComparison.OrdinalIgnoreCase))
                    {
                        customer.TongChiTieu = totalSpending;
                        customer.LoaiKhachHang = newType;
                        customer.NgayCapNhatVIP = DateTime.Now;
                        updatedCount++;
                    }
                }

                if (updatedCount > 0)
                {
                    db.SaveChanges();
                }

                return updatedCount;
            }
            catch
            {
                return 0;
            }
        }

        /// <summary>
        /// Get VIP discount for a customer
        /// </summary>
        public decimal GetVIPDiscount(int customerId)
        {
            var customer = db.KHACHHANGs.FirstOrDefault(k => k.MaKH == customerId);
            if (customer == null)
                return 0;

            return customer.LoaiKhachHang == "VIP" ? VIP_DISCOUNT_PERCENTAGE : 0;
        }

        /// <summary>
        /// Get VIP benefits summary
        /// </summary>
        public VIPBenefits GetVIPBenefits(int customerId)
        {
            var customer = db.KHACHHANGs.FirstOrDefault(k => k.MaKH == customerId);

            if (customer == null)
                return null;

            long totalSpending = CalculateTotalSpending(customerId);
            string customerType = DetermineCustomerType(totalSpending);

            if (customer.TongChiTieu != totalSpending || !string.Equals(customer.LoaiKhachHang, customerType, StringComparison.OrdinalIgnoreCase))
            {
                customer.TongChiTieu = totalSpending;
                customer.LoaiKhachHang = customerType;
                customer.NgayCapNhatVIP = DateTime.Now;
                db.SaveChanges();
            }

            bool isVIP = totalSpending >= VIP_SPENDING_THRESHOLD;

            long remaining = Math.Max(0, VIP_SPENDING_THRESHOLD - totalSpending);

            int progress = (int)Math.Min(
                totalSpending * 100L / VIP_SPENDING_THRESHOLD,
                100);

            return new VIPBenefits
            {
                IsVIP = isVIP,
                CustomerType = customerType,
                TotalSpending = totalSpending,
                DiscountPercentage = isVIP ? (int)(VIP_DISCOUNT_PERCENTAGE * 100) : 0,
                RemainingSpendingForVIP = remaining,
                LastVIPUpdateDate = customer.NgayCapNhatVIP,

                // Thông tin bổ sung
                VIPThreshold = VIP_SPENDING_THRESHOLD,
                ProgressPercentage = progress,
                IsNearVIP = !isVIP && remaining <= 1000000
            };
        }

        /// <summary>
        /// Get customer statistics for admin
        /// </summary>
        public CustomerStatistics GetCustomerStatistics()
        {
            try
            {
                var customers = db.KHACHHANGs.Where(k => k.TrangThai == true).ToList();
                var vipCustomers = customers.Where(k => k.LoaiKhachHang == "VIP").ToList();
                var regularCustomers = customers.Where(k => k.LoaiKhachHang == "Regular").ToList();

                long totalSpendingAll = customers.Sum(k => k.TongChiTieu ?? 0);
                long totalSpendingVIP = vipCustomers.Sum(k => k.TongChiTieu ?? 0);

                return new CustomerStatistics
                {
                    TotalCustomers = customers.Count,
                    VIPCustomersCount = vipCustomers.Count,
                    RegularCustomersCount = regularCustomers.Count,
                    VIPPercentage = customers.Count > 0 ? (double)vipCustomers.Count / customers.Count * 100 : 0,
                    TotalSpendingAll = totalSpendingAll,
                    TotalSpendingVIP = totalSpendingVIP,
                    AverageSpendingPerCustomer = customers.Count > 0 ? totalSpendingAll / customers.Count : 0,
                    AverageSpendingPerVIP = vipCustomers.Count > 0 ? totalSpendingVIP / vipCustomers.Count : 0
                };
            }
            catch
            {
                return null;
            }
        }

        /// <summary>
        /// Get top VIP customers by spending
        /// </summary>
        public IQueryable<KHACHHANG> GetTopVIPCustomers(int count = 10)
        {
            return db.KHACHHANGs
                .Where(k => k.LoaiKhachHang == "VIP" && k.TrangThai == true)
                .OrderByDescending(k => k.TongChiTieu)
                .Take(count);
        }
    }

    /// <summary>
    /// DTO for VIP benefits information
    /// </summary>
    public class VIPBenefits
    {
        public bool IsVIP { get; set; }

        public string CustomerType { get; set; }

        public long TotalSpending { get; set; }

        public int DiscountPercentage { get; set; }

        public long RemainingSpendingForVIP { get; set; }

        public DateTime? LastVIPUpdateDate { get; set; }

        public long VIPThreshold { get; set; }

        public int ProgressPercentage { get; set; }

        public bool IsNearVIP { get; set; }
    }

    /// <summary>
    /// DTO for customer statistics
    /// </summary>
    public class CustomerStatistics
    {
        public int TotalCustomers { get; set; }
        public int VIPCustomersCount { get; set; }
        public int RegularCustomersCount { get; set; }
        public double VIPPercentage { get; set; }
        public long TotalSpendingAll { get; set; }
        public long TotalSpendingVIP { get; set; }
        public long AverageSpendingPerCustomer { get; set; }
        public long AverageSpendingPerVIP { get; set; }
    }
}
