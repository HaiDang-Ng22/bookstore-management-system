using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BookStoreOnline.Models;

namespace BookStoreOnline.Controllers
{
    public class HomeController : Controller
    {
        NhaSachEntities3 db = new NhaSachEntities3();
        public ActionResult Index()
        {
            var topBooks = db.SANPHAMs.OrderByDescending(s => s.SoLuongBan).Take(5).ToList(); // Lấy 5 cuốn sách bán chạy nhất
            return View(topBooks);
        }
    }
}