using System;
using System.Collections.Generic;
using System.Linq;
using System.Web;
using System.Web.Mvc;
using BookStoreOnline.Models;
using BookStoreOnline.Factories; 

using BookStoreOnline.Core;
using static BookStoreOnline.Areas.Admin.Constants.Constants;

namespace BookStoreOnline.Areas.Admin.Controllers
{
    [AdminAuthorize(AdminRole.Admin)]
    public class AdminReviewController : Controller
    {
        private readonly NhaSachEntities3 db = new NhaSachEntities3();

        // GET: AdminReview
        // GET: AdminReview
        public ActionResult Index(int page = 1)
        {
            // [CẬP NHẬT] Sử dụng Factory để lấy danh sách đánh giá
            var allReviews = ReviewFactory.CreateReviews(db);

            int pageSize = 7; // Số lượng dòng trên mỗi trang
            int totalReviews = allReviews.Count();
            int totalPages = (int)Math.Ceiling((double)totalReviews / pageSize);

            // Tránh trường hợp nhập số trang vượt quá giới hạn
            if (page < 1) page = 1;
            if (page > totalPages && totalPages > 0) page = totalPages;

            var pagedReviews = allReviews
                                .Skip((page - 1) * pageSize)
                                .Take(pageSize)
                                .ToList();

            // Truyền dữ liệu phân trang qua ViewBag
            ViewBag.CurrentPage = page;
            ViewBag.PageSize = pageSize;
            ViewBag.TotalPages = totalPages;
            ViewBag.TotalCount = totalReviews;

            return View(pagedReviews);
        }

        // GET: AdminReview/Delete/5
        public ActionResult Delete(int id)
        {
            // Lấy đánh giá trực tiếp từ DB
            // var review = db.DANHGIAs.Find(id);

            // [CẬP NHẬT] Sử dụng Factory để lấy đánh giá theo ID
            var review = ReviewFactory.CreateReviewById(db, id);
            if (review == null)
            {
                return HttpNotFound();
            }
            return View(review);
        }

        // POST: AdminReview/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            // Lấy đánh giá trực tiếp từ DB
            // var review = db.DANHGIAs.Find(id);

            // [CẬP NHẬT] Sử dụng Factory để lấy đánh giá theo ID
            var review = ReviewFactory.CreateReviewById(db, id);
            if (review != null)
            {
                db.DANHGIAs.Remove(review);
                db.SaveChanges();
            }
            return RedirectToAction("Index");
        }
    }
}
