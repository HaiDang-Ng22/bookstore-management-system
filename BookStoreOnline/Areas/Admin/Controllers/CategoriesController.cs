using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using BookStoreOnline.Models;
using BookStoreOnline.Singleton;

using BookStoreOnline.Core;
using static BookStoreOnline.Areas.Admin.Constants.Constants;

namespace BookStoreOnline.Areas.Admin.Controllers
{
    [AdminAuthorize(AdminRole.Administrator, AdminRole.Manager)]
    public class CategoriesController : ControllerTemplateMethod
    {
        private readonly CategorySingleton _categorySingleton; //  Dùng Singleton cho GetAllCategories
        //private NhaSachEntities3 db = new NhaSachEntities3();

        public CategoriesController()
        {
            _categorySingleton = CategorySingleton.Instance; //  Gán instance Singleton của CategoryService
            PrintInfomation(); // Gọi in thông tin khi khởi tạo Controller
        }

        // GET: Admin/LOAIs.
        public ActionResult Index()
        {
            //return View(db.LOAIs.ToList());
            return View(_categorySingleton.GetAllCategories()); //  Lấy danh sách danh mục từ Singleton Service
        }

        // GET: Admin/LOAIs.Details/5
        public ActionResult Details(int? id)
        {
            //if (id == null)
            //{
            //    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            //}
            //LOAI loai = db.LOAIs.Find(id);
            //if (loai == null)
            //{
            //    return HttpNotFound();
            //}
            //return View(loai);

            //Singleton
            LOAI loai = _categorySingleton.GetCategoryById(id.Value);
            if (loai == null)
            {
                return HttpNotFound();
            }
            return View(loai);
        }

        // GET: Admin/LOAIs.Create
        public ActionResult Create()
        {
            return View();
        }

        // POST: Admin/LOAIs.Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "MaLoai,TenLoai")] LOAI loai)
        {
            if (ModelState.IsValid)
            {
                //db.LOAIs.Add(loai);
                //db.SaveChanges();
                //return RedirectToAction("Index");

                _categorySingleton.AddCategory(loai); // Dùng Singleton để thêm danh mục
                return RedirectToAction("Index");
            }

            return View(loai);
        }

        // GET: Admin/LOAIs.Edit/5
        public ActionResult Edit(int? id)
        {
            //if (id == null)
            //{
            //    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            //}
            //LOAI loai = db.LOAIs.Find(id);
            //if (loai == null)
            //{
            //    return HttpNotFound();
            //}
            //return View(loai);

            //Singleton
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            LOAI loai = _categorySingleton.GetCategoryById(id.Value);
            if (loai == null)
            {
                return HttpNotFound();
            }
            return View(loai);
        }

        // POST: Admin/LOAIs.Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MaLoai, TenLoai")] LOAI loai)
        {
            if (ModelState.IsValid)
            {
                //db.Entry(loai).State = EntityState.Modified;
                //db.SaveChanges();
                //return RedirectToAction("Index");

                _categorySingleton.UpdateCategory(loai); // Dùng Singleton để cập nhật danh mục
                return RedirectToAction("Index");
            }
            return View(loai);
        }

        // GET: Admin/LOAIs.Delete/5
        public ActionResult Delete(int? id)
        {
            //if (id == null)
            //{
            //    return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            //}
            //LOAI loai = db.LOAIs.Find(id);
            //if (loai == null)
            //{
            //    return HttpNotFound();
            //}
            //return View(loai);

            //Singleton
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            LOAI loai = _categorySingleton.GetCategoryById(id.Value);
            if (loai == null)
            {
                return HttpNotFound();
            }

            return View(loai);
        }

        // POST: Admin/LOAIs.Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            //LOAI loai = db.LOAIs.Find(id);
            //db.LOAIs.Remove(loai);
            //db.SaveChanges();
            //return RedirectToAction("Index");

            _categorySingleton.RemoveCategory(id); // Dùng Singleton để xóa danh mục
            return RedirectToAction("Index");
        }

        //protected override void Dispose(bool disposing)
        //{
        //    if (disposing)
        //    {
        //        db.Dispose();
        //    }
        //    base.Dispose(disposing);
        //}

        // Ghi đè PrintRouter để in thông tin route
        protected override void PrintRouter()
        {
            System.Diagnostics.Debug.WriteLine($@"{GetType().Name} Routes:
GET: Admin/LOAIs
GET: Admin/LOAIs.Details/5
POST: Admin/LOAIs.Create
GET: Admin/LOAIs.Create
GET: Admin/LOAIs.Edit/5
POST: Admin/LOAIs.Edit/5
GET: Admin/LOAIs.Delete/5
POST: Admin/LOAIs.Delete/5");
        }

        // Ghi đè PrintDIs để in dependency injection
        public override void PrintDIs()
        {
            System.Diagnostics.Debug.WriteLine($@"
Dependencies:
- NhaSachEntities3 (Database Context)");
        }
    }
}
