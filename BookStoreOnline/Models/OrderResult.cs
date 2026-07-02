using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BookStoreOnline.Models
{
    public class OrderResult
    {
        public int MaDonHang { get; set; }

        public int TrangThai { get; set; }

        public int TongTien { get; set; }

        public DateTime? NgayDat { get; set; }

        public string DiaChi { get; set; }
    }
}