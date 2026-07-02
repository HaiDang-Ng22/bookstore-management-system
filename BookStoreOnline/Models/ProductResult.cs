using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;

namespace BookStoreOnline.Models
{
    public class ProductResult
    {
        public int MaSanPham { get; set; }

        public string TenSanPham { get; set; }

        public decimal Gia { get; set; }

        public string Anh { get; set; }

        public string TacGia { get; set; }

        public int SoLuongBan { get; set; }
    }
}