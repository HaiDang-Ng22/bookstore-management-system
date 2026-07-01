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
        // GET: Admin/Products
        public ActionResult Index(string searchString, int? page)
        {

            IQueryable<SANPHAM> sanPham = db.SANPHAMs.OrderByDescending(p => p.MaSanPham);

            if (!String.IsNullOrEmpty(searchString))
            {
                sanPham = sanPham.Where(s => s.TenSanPham.Contains(searchString));
            }

            int pageSize = 7;
            int pageNumber = page ?? 1; // Nếu page null thì mặc định là trang 1
            int totalItems = sanPham.Count();
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);


            if (pageNumber < 1) pageNumber = 1;
            if (pageNumber > totalPages && totalPages > 0) pageNumber = totalPages;


            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = totalPages;
            ViewBag.CurrentFilter = searchString; // Giữ lại từ khóa tìm kiếm khi chuyển trang

 
            var pagedList = sanPham.Skip((pageNumber - 1) * pageSize).Take(pageSize).ToList();

            return View(pagedList);
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

            ViewBag.Categories = db.Database.SqlQuery<string>(
                "SELECT l.TenLoai FROM SANPHAM_LOAI sl JOIN LOAI l ON sl.MaLoai = l.MaLoai WHERE sl.MaSanPham = @p0", 
                id.Value).ToList();
            ViewBag.Volumes = db.Database.SqlQuery<VolumeDto>(
                "SELECT MaTap, TenTap, SoLuong FROM TAP_SANPHAM WHERE MaSanPham = @p0", 
                id.Value).ToList();

            return View(sanPham);
        }

        // GET: Admin/Products/Create
        public ActionResult Create()
        {
            ViewBag.LoaiSP = new SelectList(db.LOAIs, "MaLoai", "TenLoai");
            ViewBag.AllCategories = db.LOAIs.ToList();
            return View();
        }

        // POST: Admin/Products/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "MaSanPham,TenSanPham,Gia,MoTa,TacGia,Anh,MaLoai,SoLuong")] SANPHAM sanPham, HttpPostedFileBase imageBook, List<int> SelectedCategories, List<string> VolumeNames, List<int> VolumeQuantities)
        {
            if (ModelState.IsValid)
            {
                if (imageBook != null && imageBook.ContentLength > 0)
                {
                    // Upload ảnh lên Cloudinary
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(imageBook.FileName, imageBook.InputStream),
                        PublicId = "bookstore/" + Guid.NewGuid().ToString(),
                        Overwrite = true
                    };

                    var uploadResult = cloudinary.Upload(uploadParams);
                    sanPham.Anh = uploadResult.SecureUrl.ToString(); // Lưu URL ảnh từ Cloudinary vào database
                }

                // Tính tổng số lượng từ các tập nếu có
                if (VolumeQuantities != null && VolumeQuantities.Any())
                {
                    sanPham.SoLuong = VolumeQuantities.Sum();
                }

                db.SANPHAMs.Add(sanPham);
                db.SaveChanges();

                // Lưu danh sách thể loại
                if (SelectedCategories != null)
                {
                    foreach (var catId in SelectedCategories)
                    {
                        db.Database.ExecuteSqlCommand("INSERT INTO SANPHAM_LOAI (MaSanPham, MaLoai) VALUES (@p0, @p1)", sanPham.MaSanPham, catId);
                    }
                }

                // Lưu danh sách các tập sách
                if (VolumeNames != null && VolumeQuantities != null)
                {
                    for (int i = 0; i < VolumeNames.Count; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(VolumeNames[i]))
                        {
                            db.Database.ExecuteSqlCommand("INSERT INTO TAP_SANPHAM (MaSanPham, TenTap, SoLuong) VALUES (@p0, @p1, @p2)", 
                                sanPham.MaSanPham, VolumeNames[i], VolumeQuantities[i]);
                        }
                    }
                }

                return RedirectToAction("Index");
            }

            ViewBag.LoaiSP = new SelectList(db.LOAIs, "MaLoai", "TenLoai", sanPham.MaLoai);
            ViewBag.AllCategories = db.LOAIs.ToList();
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

            var selectedCats = db.Database.SqlQuery<int>("SELECT MaLoai FROM SANPHAM_LOAI WHERE MaSanPham = @p0", id.Value).ToList();
            ViewBag.SelectedCategories = selectedCats;
            ViewBag.AllCategories = db.LOAIs.ToList();

            ViewBag.Volumes = db.Database.SqlQuery<VolumeDto>("SELECT MaTap, TenTap, SoLuong FROM TAP_SANPHAM WHERE MaSanPham = @p0", id.Value).ToList();

            ViewBag.LoaiSP = new SelectList(db.LOAIs, "MaLoai", "TenLoai", sanPham.MaLoai);
            return View(sanPham);
        }

        // POST: Admin/Products/Edit/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MaSanPham,TenSanPham,Gia,MoTa,TacGia,Anh,MaLoai,SoLuong")] SANPHAM sanPham, HttpPostedFileBase imageBook, List<int> SelectedCategories, List<string> VolumeNames, List<int> VolumeQuantities, List<int> VolumeIds)
        {
            if (ModelState.IsValid)
            {
                if (imageBook != null && imageBook.ContentLength > 0)
                {
                    // Upload ảnh mới lên Cloudinary
                    var uploadParams = new ImageUploadParams()
                    {
                        File = new FileDescription(imageBook.FileName, imageBook.InputStream),
                        PublicId = "bookstore/" + Guid.NewGuid().ToString(),
                        Overwrite = true
                    };

                    var uploadResult = cloudinary.Upload(uploadParams);
                    sanPham.Anh = uploadResult.SecureUrl.ToString(); // Cập nhật URL ảnh mới vào database
                }

                // Cập nhật thể loại
                db.Database.ExecuteSqlCommand("DELETE FROM SANPHAM_LOAI WHERE MaSanPham = @p0", sanPham.MaSanPham);
                if (SelectedCategories != null)
                {
                    foreach (var catId in SelectedCategories)
                    {
                        db.Database.ExecuteSqlCommand("INSERT INTO SANPHAM_LOAI (MaSanPham, MaLoai) VALUES (@p0, @p1)", sanPham.MaSanPham, catId);
                    }
                }

                // Cập nhật tập sách
                // Lấy danh sách các MaTap hiện tại trong DB của sản phẩm này
                var currentDbVolumeIds = db.Database.SqlQuery<int>("SELECT MaTap FROM TAP_SANPHAM WHERE MaSanPham = @p0", sanPham.MaSanPham).ToList();

                // Xác định các MaTap cần xoá (có trong DB nhưng không có trong form gửi lên)
                var submittedVolumeIds = VolumeIds != null ? VolumeIds.Where(id => id > 0).ToList() : new List<int>();
                var volumeIdsToDelete = currentDbVolumeIds.Except(submittedVolumeIds).ToList();

                // Thực hiện xoá các tập không còn trong form
                if (volumeIdsToDelete.Any())
                {
                    foreach (var delId in volumeIdsToDelete)
                    {
                        db.Database.ExecuteSqlCommand("DELETE FROM TAP_SANPHAM WHERE MaTap = @p0", delId);
                    }
                }

                if (VolumeNames != null && VolumeQuantities != null)
                {
                    for (int i = 0; i < VolumeNames.Count; i++)
                    {
                        if (!string.IsNullOrWhiteSpace(VolumeNames[i]))
                        {
                            if (VolumeIds != null && i < VolumeIds.Count && VolumeIds[i] > 0)
                            {
                                // Cập nhật tập hiện có
                                db.Database.ExecuteSqlCommand("UPDATE TAP_SANPHAM SET TenTap=@p0, SoLuong=@p1 WHERE MaTap=@p2",
                                    VolumeNames[i], VolumeQuantities[i], VolumeIds[i]);
                            }
                            else
                            {
                                // Thêm tập mới
                                db.Database.ExecuteSqlCommand("INSERT INTO TAP_SANPHAM (MaSanPham, TenTap, SoLuong) VALUES (@p0, @p1, @p2)",
                                    sanPham.MaSanPham, VolumeNames[i], VolumeQuantities[i]);
                            }
                        }
                    }
                }

                // Update tổng số lượng
                int totalVolQty = db.Database.SqlQuery<int>("SELECT ISNULL(SUM(SoLuong), 0) FROM TAP_SANPHAM WHERE MaSanPham = @p0", sanPham.MaSanPham).FirstOrDefault();
                sanPham.SoLuong = totalVolQty;

                db.Entry(sanPham).State = EntityState.Modified;
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            var selectedCats = db.Database.SqlQuery<int>("SELECT MaLoai FROM SANPHAM_LOAI WHERE MaSanPham = @p0", sanPham.MaSanPham).ToList();
            ViewBag.SelectedCategories = selectedCats;
            ViewBag.AllCategories = db.LOAIs.ToList();
            ViewBag.Volumes = db.Database.SqlQuery<VolumeDto>("SELECT MaTap, TenTap, SoLuong FROM TAP_SANPHAM WHERE MaSanPham = @p0", sanPham.MaSanPham).ToList();

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

                // Xóa các tập và thể loại liên quan trước khi xóa sản phẩm
                db.Database.ExecuteSqlCommand("DELETE FROM TAP_SANPHAM WHERE MaSanPham = @p0", sanPham.MaSanPham);
                db.Database.ExecuteSqlCommand("DELETE FROM SANPHAM_LOAI WHERE MaSanPham = @p0", sanPham.MaSanPham);

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
                        PublicId = "bookstore/" + Guid.NewGuid().ToString(),
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
