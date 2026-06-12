using BookStoreOnline.Core;
using BookStoreOnline.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;

using static BookStoreOnline.Areas.Admin.Constants.Constants;

namespace BookStoreOnline.Areas.Admin.Controllers
{
    [AdminAuthorize(AdminRole.Administrator, AdminRole.Manager)]
    public class ControlUserController : Controller
    {
        private NhaSachEntities3 db = new NhaSachEntities3();
        // GET: Admin/ControlUser
        public ActionResult Index()
        {
            var users = db.KHACHHANGs.ToList();
            return View(users);
        }
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            KHACHHANG khachHang = db.KHACHHANGs.Find(id);
            if (khachHang == null)
            {
                return HttpNotFound();
            }
            return View(khachHang);
        }

        // POST: Admin/Nguoidungs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            KHACHHANG nguoidung = db.KHACHHANGs.Find(id);

            db.KHACHHANGs.Remove(nguoidung);
            db.SaveChanges();
            return RedirectToAction("Index");
        }
        public ActionResult ResetPassword(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            KHACHHANG khachHang = db.KHACHHANGs.Find(id);
            if (khachHang == null)
            {
                return HttpNotFound();
            }
            return View(khachHang);
        }

        [HttpPost, ActionName("ResetPassword")]
        [ValidateAntiForgeryToken]
        public ActionResult ResetPassword(int id)
        {
            KHACHHANG nguoidung = db.KHACHHANGs.Find(id);

            nguoidung.MatKhau = "123456";
               
            db.SaveChanges();
            return RedirectToAction("Index");
        }

        public ActionResult DisableAccount(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            KHACHHANG KHACHHANGs = db.KHACHHANGs.Find(id);
            if (KHACHHANGs == null)
            {
                return HttpNotFound();
            }
            return View(KHACHHANGs);
        }

        [HttpPost, ActionName("DisableAccount")]
        [ValidateAntiForgeryToken]
        public ActionResult DisableAccountConfirmed(int id)
        {
            KHACHHANG khachHang = db.KHACHHANGs.Find(id);
            if (khachHang == null)
            {
                return HttpNotFound();
            }

            khachHang.TrangThai = false; 
            db.SaveChanges();

            return RedirectToAction("Index");
        }

        public ActionResult EnableAccount(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            KHACHHANG KHACHHANGs = db.KHACHHANGs.Find(id);
            if (KHACHHANGs == null)
            {
                return HttpNotFound();
            }
            return View(KHACHHANGs);
        }

        [HttpPost, ActionName("EnableAccount")]
        [ValidateAntiForgeryToken]
        public ActionResult EnableAccount(int id)
        {
            KHACHHANG khachHang = db.KHACHHANGs.Find(id);
            if (khachHang == null)
            {
                return HttpNotFound();
            }

            khachHang.TrangThai = true;
            db.SaveChanges();

            return RedirectToAction("Index");
        }

    }
}