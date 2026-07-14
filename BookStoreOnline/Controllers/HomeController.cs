using System.Linq;
using System.Web.Mvc;
using BookStoreOnline.Models;

namespace BookStoreOnline.Controllers
{
    public class HomeController : Controller
    {
        private readonly NhaSachEntities3 db = new NhaSachEntities3();

        public ActionResult Index(int? minPrice, int? maxPrice, string sortOrder)
        {
            ViewBag.MinPrice = minPrice;
            ViewBag.MaxPrice = maxPrice;
            ViewBag.SortOrder = sortOrder;

            var query = db.SANPHAMs.AsQueryable();

            if (minPrice.HasValue) query = query.Where(x => x.Gia >= minPrice.Value);
            if (maxPrice.HasValue) query = query.Where(x => x.Gia <= maxPrice.Value);

            if (sortOrder == "price_asc")
            {
                query = query.OrderBy(x => x.Gia);
            }
            else if (sortOrder == "price_desc")
            {
                query = query.OrderByDescending(x => x.Gia);
            }
            else
            {
                query = query.OrderByDescending(x => x.MaSanPham);
            }

            int takeCount = (minPrice.HasValue || maxPrice.HasValue || !string.IsNullOrEmpty(sortOrder)) ? 24 : 8;
            var books = query.Take(takeCount).ToList();

            return View(books);
        }
    }
}