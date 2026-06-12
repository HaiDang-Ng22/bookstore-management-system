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
    [AdminAuthorize(AdminRole.Administrator, AdminRole.Manager)]
    public class AdminReviewController : Controller
    {
        private readonly NhaSachEntities3 db = new NhaSachEntities3();

        // GET: AdminReview
        public ActionResult Index()
        {
            // Lấy danh sách đánh giá trực tiếp từ DB
            // var reviews = db.DANHGIAs.Include("KHACHHANG").Include("SANPHAM").ToList();

            // [CẬP NHẬT] Sử dụng Factory để lấy danh sách đánh giá
            var reviews = ReviewFactory.CreateReviews(db);
            return View(reviews);
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
