using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Web.Mvc;
using BookStoreOnline.Models;

namespace BookStoreOnline.Controllers
{
    public class CategoryController : Controller
    {
        private NhaSachEntities3 db = new NhaSachEntities3();

        // Bỏ dấu tiếng Việt
        private string RemoveDiacritics(string text)
        {
            if (string.IsNullOrEmpty(text))
                return "";

            string normalizedString = text.Normalize(NormalizationForm.FormD);
            StringBuilder stringBuilder = new StringBuilder();

            foreach (char c in normalizedString)
            {
                if (CharUnicodeInfo.GetUnicodeCategory(c) != UnicodeCategory.NonSpacingMark)
                {
                    stringBuilder.Append(c);
                }
            }

            return stringBuilder.ToString()
                .Replace('đ', 'd')
                .Replace('Đ', 'D')
                .Normalize(NormalizationForm.FormC);
        }

        // Hiển thị sách theo loại
        public ActionResult Index(int id)
        {
            ViewBag.CategoryName = db.LOAIs
                .FirstOrDefault(x => x.Maloai == id)?.Tenloai;

            var products = db.SANPHAMs
                .Where(x => x.MaLoai == id)
                .ToList();

            return View(products);
        }

        // Hiển thị tất cả sách
        public ActionResult GetAllBook()
        {
            return View(db.SANPHAMs.ToList());
        }

        // Tìm kiếm
        public ActionResult Search(string inputString)
        {
            ViewBag.TextSearch = inputString;

            if (string.IsNullOrWhiteSpace(inputString))
            {
                return View("Search", new List<SANPHAM>());
            }

            string keyword = RemoveDiacritics(inputString.Trim().ToLower());

            var result = db.SANPHAMs
                .ToList()
                .Where(s =>
                    RemoveDiacritics(s.TenSanPham ?? "")
                        .ToLower()
                        .Contains(keyword)

                    ||

                    RemoveDiacritics(s.TacGia ?? "")
                        .ToLower()
                        .Contains(keyword)

                    ||

                    (s.LOAI != null && RemoveDiacritics(s.LOAI.Tenloai ?? "")
                        .ToLower()
                        .Contains(keyword))
                )
                .ToList();

            return View("Search", result);
        }
    }
}