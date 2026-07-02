using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Web;

namespace BookStoreOnline.Areas.Admin.Constants
{
    public static class Constants
    {
        public enum AdminRole
        {
            [Description("Admin")]
            Admin = 1,
            [Description("User")]
            User = 2,
            [Description("Shipper")]
            Shipper = 3
        }

        public enum AccountType
        {
            [Description("Nhân viên")]
            Staff = 1,
            [Description("Khách hàng")]
            Customer = 2
        }
        public enum StatusOrder
        {
            [Description("Chưa xác nhận")]
            NoInform = 0,
            [Description("Đã xác nhận")]
            Informed = 1,
            [Description("Đang giao hàng")]
            Shipping = 2,
            [Description("Đã nhận hàng")]
            Received = 3,
            [Description("Đã hủy")]
            Canceled = 4
        }
        public enum StatusPayment
        {
            [Description("Chưa thanh toán")]
            Unpaid = 0,
            [Description("Đã thanh toán")]
            Paid = 1,
            Refund = 2
        }
        public enum TypePayment
        {
            [Description("Tiền mặt")]
            COD = 1,
            [Description("Ngân Hàng")]
            VNPAY = 2,
            [Description("Ví điện tử")]
            Bank = 3
        }

    }
}
