using System;
using System.Collections.Generic;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web.Mvc;
using BookStoreOnline.Models;
using BookStoreOnline.Services;

using BookStoreOnline.Core;
using static BookStoreOnline.Areas.Admin.Constants.Constants;

namespace BookStoreOnline.Areas.Admin.Controllers
{
    [AdminAuthorize(AdminRole.Administrator, AdminRole.Manager)]
    public class DiscountController : Controller
    {
        //private NhaSachEntities3 db = new NhaSachEntities3();

        private readonly PromotionFacade _promotionFacade; // dùng Facade

        public DiscountController()
        {
            _promotionFacade = new PromotionFacade(); // dùng Facade
        }

        // GET: KhuyenMai
        public ActionResult Index()
        {
            //return View(db.KHUYENMAIs.ToList());
            return View(_promotionFacade.GetAllPromotions()); // dùng Facade
        }

        // GET: KhuyenMai/Details/5
        public ActionResult Details(int id)
        {
            if (id == 0)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            //KHUYENMAI khuyenMai = db.KHUYENMAIs.Find(id);
            KHUYENMAI khuyenMai = _promotionFacade.GetPromotionById(id); //  dùng Facade

            if (khuyenMai == null)
            {
                return HttpNotFound();
            }

            return View(khuyenMai);
        }

        // GET: KhuyenMai/Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: KhuyenMai/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "ID,TenChuongTrinh,MaKM,MoTa,NgayTao,NgayBatDau,NgayKetThuc,SoTienKM,SoTienMuaHangToiThieu,SoLanDung,KichHoat")] KHUYENMAI khuyenMai)
        {
            if (ModelState.IsValid)
            {
                //db.KHUYENMAIs.Add(khuyenMai);
                //db.SaveChanges();

                _promotionFacade.AddPromotion(khuyenMai); //  dùng Facade

                return RedirectToAction("Index");
            }

            return View(khuyenMai);
        }

        // GET: KhuyenMai/Edit/5
        public ActionResult Edit(int id)
        {
            if (id == 0)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            //KHUYENMAI khuyenMai = db.KHUYENMAIs.Find(id);
            KHUYENMAI khuyenMai = _promotionFacade.GetPromotionById(id); //  dùng Facade

            if (khuyenMai == null)
            {
                return HttpNotFound();
            }

            return View(khuyenMai);
        }

        // POST: KhuyenMai/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "ID,TenChuongTrinh,MaKM,MoTa,NgayTao,NgayBatDau,NgayKetThuc,SoTienKM,SoTienMuaHangToiThieu,SoLanDung,KichHoat")] KHUYENMAI khuyenMai)
        {
            if (ModelState.IsValid)
            {
                //db.Entry(khuyenMai).State = EntityState.Modified;
                //db.SaveChanges();

                _promotionFacade.UpdatePromotion(khuyenMai); // dùng Facade

                return RedirectToAction("Index");
            }

            return View(khuyenMai);
        }

        // GET: KhuyenMai/Delete/5
        public ActionResult Delete(int id)
        {
            if (id == 0)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            //KHUYENMAI khuyenMai = db.KHUYENMAIs.Find(id);
            KHUYENMAI khuyenMai = _promotionFacade.GetPromotionById(id); //  dùng Facade

            if (khuyenMai == null)
            {
                return HttpNotFound();
            }

            return View(khuyenMai);
        }

        // POST: KhuyenMai/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            //KHUYENMAI khuyenMai = db.KHUYENMAIs.Find(id);
            //db.KHUYENMAIs.Remove(khuyenMai);
            //db.SaveChanges();

            _promotionFacade.DeletePromotion(id); // dùng Facade

            return RedirectToAction("Index");
        }

        [HttpPost]
        public JsonResult ToggleActivation(int id, bool isActive)
        {
            try
            {
                //var khuyenMai = db.KHUYENMAIs.Find(id);
                //if (khuyenMai == null)
                //{
                //    return Json(new { success = false, message = "Không tìm thấy chương trình khuyến mãi." });
                //}

                //khuyenMai.KichHoat = isActive;
                //db.SaveChanges();

                _promotionFacade.TogglePromotionActivation(id, isActive); //  dùng Facade

                return Json(new { success = true });
            }
            catch (Exception ex)
            {
                return Json(new { success = false, message = "Lỗi: " + ex.Message });
            }
        }

        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                //db.Dispose();
                _promotionFacade.Dispose(); // dùng Facade
            }
            base.Dispose(disposing);
        }
    }
}
