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
    [AdminAuthorize(AdminRole.Admin)]
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
        public ActionResult Create([Bind(Include = "MaSanPham,TenSanPham,Gia,MoTa,TacGia,Anh,MaLoai,SoLuong,GiamGia")] SANPHAM sanPham, HttpPostedFileBase imageBook, List<int> SelectedCategories, List<string> VolumeNames, List<int> VolumeQuantities)
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

                // Nếu lúc tạo có nhập giảm giá sẵn, tính luôn giá đã giảm vào cột Gia
                if (sanPham.GiamGia.HasValue && sanPham.GiamGia.Value > 0 && sanPham.Gia.HasValue)
                {
                    sanPham.Gia = sanPham.Gia.Value - (sanPham.Gia.Value * sanPham.GiamGia.Value / 100);
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
        public ActionResult Edit([Bind(Include = "MaSanPham,TenSanPham,Gia,MoTa,TacGia,Anh,MaLoai,SoLuong,GiamGia")] SANPHAM sanPham, HttpPostedFileBase imageBook, List<int> SelectedCategories, List<string> VolumeNames, List<int> VolumeQuantities, List<int> VolumeIds)
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
                var currentDbVolumeIds = db.Database.SqlQuery<int>("SELECT MaTap FROM TAP_SANPHAM WHERE MaSanPham = @p0", sanPham.MaSanPham).ToList();
                var submittedVolumeIds = VolumeIds != null ? VolumeIds.Where(id => id > 0).ToList() : new List<int>();
                var volumeIdsToDelete = currentDbVolumeIds.Except(submittedVolumeIds).ToList();

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
                                db.Database.ExecuteSqlCommand("UPDATE TAP_SANPHAM SET TenTap=@p0, SoLuong=@p1 WHERE MaTap=@p2",
                                    VolumeNames[i], VolumeQuantities[i], VolumeIds[i]);
                            }
                            else
                            {
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
                if (!string.IsNullOrEmpty(sanPham.Anh))
                {
                    var publicId = Path.GetFileNameWithoutExtension(new Uri(sanPham.Anh).AbsolutePath);
                    var deletionParams = new DeletionParams("bookstore/" + publicId);
                    cloudinary.Destroy(deletionParams);
                }

                db.Database.ExecuteSqlCommand("DELETE FROM TAP_SANPHAM WHERE MaSanPham = @p0", sanPham.MaSanPham);
                db.Database.ExecuteSqlCommand("DELETE FROM SANPHAM_LOAI WHERE MaSanPham = @p0", sanPham.MaSanPham);

                db.SANPHAMs.Remove(sanPham);
                db.SaveChanges();
            }

            return RedirectToAction("Index");
        }

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

            SANPHAM clonedBook = new SANPHAM
            {
                TenSanPham = "Bản sao của " + originalBook.TenSanPham,
                TacGia = originalBook.TacGia,
                Gia = originalBook.Gia,
                MoTa = originalBook.MoTa,
                Anh = originalBook.Anh,
                MaLoai = originalBook.MaLoai,
                SoLuong = originalBook.SoLuong,
                GiamGia = originalBook.GiamGia, // Giữ nguyên tỉ lệ giảm giá khi sao chép
                SoLuongBan = 0,
                MaSanPham = 0
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

                db.SANPHAMs.Add(model);
                db.SaveChanges();
                return RedirectToAction("Index");
            }

            return View(model);
        }

        // POST: Admin/Products/UpdateDiscount
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult UpdateDiscount(int maSanPham, int phanTramGiam)
        {
            if (phanTramGiam < 0 || phanTramGiam > 100)
            {
                TempData["Error"] = "Phần trăm giảm giá phải nằm trong khoảng từ 0 đến 100%!";
                return RedirectToAction("Index");
            }

            var sanPham = db.SANPHAMs.Find(maSanPham);
            if (sanPham != null)
            {
                // 1. Phục hồi về Giá Gốc trước khi tính toán mức giảm mới
                int phanTramCu = sanPham.GiamGia ?? 0;
                decimal giaHienTai = sanPham.Gia ?? 0;
                decimal giaGocBanDau = giaHienTai;

                // Nếu sản phẩm đã từng giảm giá trước đó, tính ngược lại để tìm Giá Gốc
                if (phanTramCu > 0 && phanTramCu < 100)
                {
                    giaGocBanDau = giaHienTai / (1 - ((decimal)phanTramCu / 100));
                }

                // 2. Trừ thẳng tiền vào trường Gia theo phần trăm mới
                if (phanTramGiam > 0)
                {
                    sanPham.Gia = giaGocBanDau - (giaGocBanDau * phanTramGiam / 100);
                    sanPham.GiamGia = phanTramGiam;

                    TempData["Success"] = $"Đã giảm {phanTramGiam}%. Giá mới: {string.Format("{0:N0}", sanPham.Gia)} đ (Gốc: {string.Format("{0:N0}", giaGocBanDau)} đ)";
                }
                else
                {
                    // Nếu phanTramGiam == 0, hoàn tác về Giá Gốc ban đầu
                    sanPham.Gia = giaGocBanDau;
                    sanPham.GiamGia = 0;

                    TempData["Success"] = $"Đã huỷ giảm giá. Giá khôi phục: {string.Format("{0:N0}", sanPham.Gia)} đ";
                }

                db.Entry(sanPham).State = EntityState.Modified;
                db.SaveChanges();
            }
            else
            {
                TempData["Error"] = "Không tìm thấy sản phẩm.";
            }

            return RedirectToAction("Index");
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