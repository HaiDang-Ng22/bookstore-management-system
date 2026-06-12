using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.IO;
using System.Web.Mvc;
using BookStoreOnline.Models;
using CloudinaryDotNet;
using CloudinaryDotNet.Actions;

using BookStoreOnline.Core;
using static BookStoreOnline.Areas.Admin.Constants.Constants;

namespace BookStoreOnline.Areas.Admin.Controllers
{
    [AdminAuthorize(AdminRole.Administrator, AdminRole.Manager)]
    public class ProductsController : Controller
    {
        private NhaSachEntities3 db = new NhaSachEntities3();

        // Cấu hình Cloudinary
        private Cloudinary cloudinary;

        public ProductsController()
        {
            var account = new Account(
                "dfela1rxa",    // Thay bằng Cloud Name của bạn
                "946317742558943",       // Thay bằng API Key của bạn
                "0bILZnhAynfc8n4loa5yrdaiCWw"     // Thay bằng API Secret của bạn
            );
            cloudinary = new Cloudinary(account);
        }

        // GET: Admin/Products
        public ActionResult Index(string searchString)
        {
            IQueryable<SANPHAM> sanPham = db.SANPHAMs.OrderByDescending(p => p.MaSanPham);

            if (!String.IsNullOrEmpty(searchString))
            {
                sanPham = sanPham.Where(s => s.TenSanPham.Contains(searchString));
            }

            return View(sanPham.ToList());
        }

        // GET: Admin/Products/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            SANPHAM sanPham = db.SANPHAMs.Find(id);
            if (sanPham == null)
            {
                return HttpNotFound();
            }
            return View(sanPham);
        }

        // GET: Admin/Products/Create
        public ActionResult Create()
        {
            ViewBag.LoaiSP = new SelectList(db.LOAIs, "MaLoai", "TenLoai");
            return View();
        }

        // POST: Admin/Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "MaSanPham,TenSanPham,Gia,MoTa,TacGia,Anh,MaLoai,SoLuong")] SANPHAM sanPham, HttpPostedFileBase imageBook)
        {
            if (ModelState.IsValid)
            {
                if (imageBook != null && imageBook.ContentLength > 0)
                {
                    // Upload ảnh lên Cloudinary
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(imageBook.FileName, imageBook.InputStream),
                        PublicId = "bookstore/" + Path.GetFileNameWithoutExtension(imageBook.FileName),
                        Overwrite = true
                    };

                    var uploadResult = cloudinary.Upload(uploadParams);
                    sanPham.Anh = uploadResult.SecureUrl.ToString(); // Lưu URL ảnh từ Cloudinary vào database
                }

                db.SANPHAMs.Add(sanPham);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.LoaiSP = new SelectList(db.LOAIs, "MaLoai", "TenLoai", sanPham.MaLoai);
            return View(sanPham);
        }

        // GET: Admin/Products/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            SANPHAM sanPham = db.SANPHAMs.Find(id);
            if (sanPham == null)
            {
                return HttpNotFound();
            }

            ViewBag.LoaiSP = new SelectList(db.LOAIs, "MaLoai", "TenLoai", sanPham.MaLoai);
            return View(sanPham);
        }

        // POST: Admin/Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MaSanPham,TenSanPham,Gia,MoTa,TacGia,Anh,MaLoai,SoLuong")] SANPHAM sanPham, HttpPostedFileBase imageBook)
        {
            if (ModelState.IsValid)
            {
                if (imageBook != null && imageBook.ContentLength > 0)
                {
                    // Upload ảnh mới lên Cloudinary
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(imageBook.FileName, imageBook.InputStream),
                        PublicId = "bookstore/" + Path.GetFileNameWithoutExtension(imageBook.FileName),
                        Overwrite = true
                    };

                    var uploadResult = cloudinary.Upload(uploadParams);
                    sanPham.Anh = uploadResult.SecureUrl.ToString(); // Cập nhật URL ảnh mới vào database
                }

                db.Entry(sanPham).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            ViewBag.LoaiSP = new SelectList(db.LOAIs, "MaLoai", "TenLoai", sanPham.MaLoai);
            return View(sanPham);
        }

        // GET: Admin/Products/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            SANPHAM sanPham = db.SANPHAMs.Find(id);
            if (sanPham == null)
            {
                return HttpNotFound();
            }
            return View(sanPham);
        }

        // POST: Admin/Products/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            SANPHAM sanPham = db.SANPHAMs.Find(id);

            if (sanPham != null)
            {
                // Xóa ảnh trên Cloudinary (nếu có)
                if (!string.IsNullOrEmpty(sanPham.Anh))
                {
                    var publicId = Path.GetFileNameWithoutExtension(new Uri(sanPham.Anh).AbsolutePath);
                    var deletionParams = new DeletionParams("bookstore/" + publicId);
                    cloudinary.Destroy(deletionParams);
                }

                db.SANPHAMs.Remove(sanPham);
                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }
        // GET: Admin/Products/Clone/5
        // GET: Admin/Products/Clone/5
        // GET: Admin/Products/Clone/5
        public ActionResult Clone(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }

            SANPHAM originalBook = db.SANPHAMs.Find(id);
            if (originalBook == null)
            {
                return HttpNotFound();
            }

            // Tạo bản sao với đầy đủ thông tin
            SANPHAM clonedBook = new SANPHAM
            {
                TenSanPham = "Bản sao của " + originalBook.TenSanPham,
                TacGia = originalBook.TacGia, // Giữ nguyên tác giả
                Gia = originalBook.Gia,       // Giữ nguyên giá
                MoTa = originalBook.MoTa,     // Giữ nguyên mô tả
                Anh = originalBook.Anh,       // Giữ nguyên ảnh
                MaLoai = originalBook.MaLoai, // Giữ nguyên thể loại
                SoLuong = originalBook.SoLuong, // Giữ nguyên số lượng
                SoLuongBan = 0,               // Reset số lượng bán
                MaSanPham = 0                 // ID mới sẽ tự sinh
            };

            ViewBag.OriginalName = originalBook.TenSanPham;
            return View(clonedBook);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Clone(SANPHAM model, HttpPostedFileBase imageBook)
        {
            if (ModelState.IsValid)
            {
                // Xử lý upload ảnh mới nếu có
                if (imageBook != null && imageBook.ContentLength > 0)
                {
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(imageBook.FileName, imageBook.InputStream),
                        PublicId = "bookstore/" + Path.GetFileNameWithoutExtension(imageBook.FileName),
                        Overwrite = true
                    };
                    var uploadResult = cloudinary.Upload(uploadParams);
                    model.Anh = uploadResult.SecureUrl.ToString();
                }
                // Nếu không có ảnh mới, giữ nguyên ảnh cũ (đã được bind từ form)

                db.SANPHAMs.Add(model);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(model);
        }
        protected override void Dispose(bool disposing)
        {
            if (disposing)
            {
                db.Dispose();
            }
            base.Dispose(disposing);
        }
    }
}
