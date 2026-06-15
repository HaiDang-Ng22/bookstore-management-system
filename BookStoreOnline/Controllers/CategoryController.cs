using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BookStoreOnline.Models;

namespace BookStoreOnline.Controllers
{
    public class CategoryController : Controller
    {
        NhaSachEntities3 db = new NhaSachEntities3();

        // GET: Category
        public ActionResult Index(int? id)
        {
            if (id == null)
            {
                return RedirectToAction("Index", "Home");
            }

            var danhMuc = db.LOAIs.FirstOrDefault(n => n.Maloai == id);
            if (danhMuc == null)
            {
                return HttpNotFound();
            }

            ViewBag.CategoryName = danhMuc.Tenloai;

            var sachTheoDanhMuc = db.SANPHAMs
                                    .Where(book => book.MaLoai == id)
                                    .OrderByDescending(book => book.MaSanPham)
                                    .ToList();

            return View(sachTheoDanhMuc);
        }

        public ActionResult GetAllBook()
        {
            return View(db.SANPHAMs.ToList());
        }

        public ActionResult Search(string inputString)
        {
            ViewBag.TextSearch = inputString;

            if (string.IsNullOrEmpty(inputString))
            {
                return View("Search", new List<SANPHAM>());
            }

            var result = db.SANPHAMs
                .Where(s => s.TenSanPham.Contains(inputString) ||
                            (s.TacGia != null && s.TacGia.Contains(inputString)) ||
                            (s.LOAI != null && s.LOAI.Tenloai.Contains(inputString)))
                .ToList();

            return View("Search", result);
        }
    }
}