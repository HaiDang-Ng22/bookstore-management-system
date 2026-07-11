using System.Linq;
using System.Web.Mvc;
using BookStoreOnline.Models;

namespace BookStoreOnline.Controllers
{
    public class HomeController : Controller
    {
        private readonly NhaSachEntities3 db = new NhaSachEntities3();

        public ActionResult Index()
        {
            var books = db.SANPHAMs.Take(8).ToList();

            return View(books);
            var topBooks = db.SANPHAMs.OrderByDescending(s => s.SoLuongBan).Take(5).ToList(); // Lấy 5 cuốn sách bán chạy nhất
            return View(topBooks);
        }
    }
}