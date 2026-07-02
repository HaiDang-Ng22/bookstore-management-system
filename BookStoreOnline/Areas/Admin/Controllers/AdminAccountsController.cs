using System;
using System.Collections.Generic;
using System.Data;
using System.Data.Entity;
using System.Linq;
using System.Net;
using System.Web;
using System.Web.Mvc;
using BookStoreOnline.Areas.Admin.Constants;
using BookStoreOnline.Core;
using BookStoreOnline.Models;
using static BookStoreOnline.Areas.Admin.Constants.Constants;

using BookStoreOnline.Core;
using static BookStoreOnline.Areas.Admin.Constants.Constants;

namespace BookStoreOnline.Areas.Admin.Controllers
{
    [AdminAuthorize(AdminRole.Admin)]
    public class AdminAccountsController : Controller
    {
        private NhaSachEntities3 db = new NhaSachEntities3();

        // GET: Admin/NHANVIENs
        // GET: Admin/NHANVIENs
        public ActionResult Index(int? page)
        {
            // 1. Xác định số dòng tối đa trên 1 trang
            int pageSize = 7;

            // 2. Xác định trang hiện tại (nếu null thì mặc định là trang 1)
            int pageNumber = (page ?? 1);

            // 3. Lấy tổng số lượng bản ghi để tính tổng số trang ở View
            int totalItems = db.NHANVIENs.Count();

            // Tính tổng số trang (ép kiểu để chia lấy trần, ví dụ 15 dòng / 7 = 3 trang)
            int totalPages = (int)Math.Ceiling((double)totalItems / pageSize);

            // 4. Truy vấn phân trang bằng Skip và Take
            // Lưu ý: Entity Framework yêu cầu phải OrderBy trước khi Skip
            var danhSachNhanVien = db.NHANVIENs
                                     .OrderBy(nv => nv.MaNV)
                                     .Skip((pageNumber - 1) * pageSize)
                                     .Take(pageSize)
                                     .ToList();

            // 5. Gửi các thông số phân trang sang View qua ViewBag
            ViewBag.CurrentPage = pageNumber;
            ViewBag.TotalPages = totalPages;
            ViewBag.PageSize = pageSize;

            var roleService = new RoleService(db);
            ViewBag.RoleNames = roleService.GetAllRoles().ToDictionary(r => r.MaVaiTro, r => r.TenVaiTro);

            return View(danhSachNhanVien);
        }

        // GET: Admin/NHANVIENs/Details/5
        public ActionResult Details(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            NHANVIEN nhanVien = db.NHANVIENs.Find(id);
            if (nhanVien == null)
            {
                return HttpNotFound();
            }
            return View(nhanVien);
        }

        // GET: Admin/NHANVIENs/Create
        public ActionResult Create()
        {
            var roleService = new RoleService(db);
            var staffRoles = roleService.GetStaffRoles()
                .Select(r => new { Id = r.MaVaiTro, Name = r.TenVaiTro });

            ViewBag.Role = new SelectList(staffRoles, "Id", "Name");
            return View();
        }

        // POST: Admin/NHANVIENs/Create
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Create([Bind(Include = "Ten,Email,Quyen")] NHANVIEN nhanVienMoi)
        {
            if (ModelState.IsValid)
            {
                var roleService = new RoleService(db);
                if (!roleService.IsValidStaffRole(nhanVienMoi.Quyen))
                {
                    ModelState.AddModelError("Quyen", "Vai trò không hợp lệ.");
                }
                else
                {
                // Extract the part before the '@' symbol
                var emailParts = nhanVienMoi.Email.Split('@');
                if (emailParts.Length > 0)
                {
                    nhanVienMoi.MatKhau = emailParts[0]; // Set the default password to the part before the '@'
                }
                else
                {
                    nhanVienMoi.MatKhau = "defaultPassword"; // Fallback in case email is invalid, adjust as needed
                }

                nhanVienMoi.NgayTao = DateTime.Now;
                nhanVienMoi.TrangThai = true;
                db.NHANVIENs.Add(nhanVienMoi);
                db.SaveChanges();
                return RedirectToAction("Index");
                }
            }

            var roleServiceFallback = new RoleService(db);
            var staffRoles = roleServiceFallback.GetStaffRoles()
                .Select(r => new { Id = r.MaVaiTro, Name = r.TenVaiTro });
            ViewBag.Role = new SelectList(staffRoles, "Id", "Name");

            return View(nhanVienMoi);
        }

        // GET: Admin/NHANVIENs/Edit/5
        public ActionResult Edit(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            NHANVIEN nhanVien = db.NHANVIENs.Find(id);
            if (nhanVien == null)
            {
                return HttpNotFound();
            }
            var roleService = new RoleService(db);
            var staffRoles = roleService.GetStaffRoles()
                .Select(r => new { Id = r.MaVaiTro, Name = r.TenVaiTro });

            ViewBag.Role = new SelectList(staffRoles, "Id", "Name", nhanVien.Quyen);
            return View(nhanVien);
        }

        // POST: Admin/NHANVIENs/Edit/5
        // To protect from overposting attacks, enable the specific properties you want to bind to, for 
        // more details see https://go.microsoft.com/fwlink/?LinkId=317598.
        [HttpPost]
        [ValidateAntiForgeryToken]
        public ActionResult Edit([Bind(Include = "MaNV,Ten,Email,Quyen,TrangThai")] NHANVIEN nhanVien)
        {
            if (ModelState.IsValid)
            {
                var roleService = new RoleService(db);
                if (!roleService.IsValidStaffRole(nhanVien.Quyen))
                {
                    ModelState.AddModelError("Quyen", "Vai trò không hợp lệ.");
                }
                else
                {
                Iterator iterator = new AdminAccountsIterator(db.NHANVIENs.ToList());
                var dbNhanVien = iterator.First();
                while (!iterator.IsDone)
                {
                    if (dbNhanVien.MaNV == nhanVien.MaNV)
                    {
                        dbNhanVien.Ten = nhanVien.Ten;
                        dbNhanVien.Quyen = nhanVien.Quyen;
                        dbNhanVien.Email = nhanVien.Email;
                        dbNhanVien.TrangThai = nhanVien.TrangThai;
                        db.Entry(dbNhanVien).State = EntityState.Modified;
                        db.SaveChanges();
                        return RedirectToAction("Index");
                    }
                    dbNhanVien = iterator.Next();
                }
                }
            }

            var roleServiceFallback = new RoleService(db);
            var staffRolesFallback = roleServiceFallback.GetStaffRoles()
                .Select(r => new { Id = r.MaVaiTro, Name = r.TenVaiTro });
            ViewBag.Role = new SelectList(staffRolesFallback, "Id", "Name", nhanVien.Quyen);

            return View(nhanVien);
        }

        // GET: Admin/NHANVIENs/Delete/5
        public ActionResult Delete(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            NHANVIEN nhanVien = db.NHANVIENs.Find(id);
            if (nhanVien == null)
            {
                return HttpNotFound();
            }
            return View(nhanVien);
        }

        // POST: Admin/NHANVIENs/Delete/5
        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public ActionResult DeleteConfirmed(int id)
        {
            // Tìm nhân viên cần xóa
            NHANVIEN nHANVIEN = db.NHANVIENs.Find(id);

            if (nHANVIEN != null)
            {
                // Xóa các bản ghi liên quan trong DONHANG
                var donHangList = db.DONHANGs.Where(dh => dh.ID == id).ToList();
                foreach (var donHang in donHangList)
                {
                    // Xóa chi tiết đơn hàng
                    var chiTietDonHangList = db.CHITIETDONHANGs.Where(ct => ct.MaDonHang == donHang.MaDonHang).ToList();
                    foreach (var chiTiet in chiTietDonHangList)
                    {
                        db.CHITIETDONHANGs.Remove(chiTiet);
                    }
                    db.DONHANGs.Remove(donHang);
                }

                // Xóa khách hàng
                db.NHANVIENs.Remove(nHANVIEN);
                db.SaveChanges();
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

        public ActionResult DisableAccount(int? id)
        {
            if (id == null)
            {
                return new HttpStatusCodeResult(HttpStatusCode.BadRequest);
            }
            NHANVIEN nhanVien = db.NHANVIENs.Find(id);
            if (nhanVien == null)
            {
                return HttpNotFound();
            }
            return View(nhanVien);
        }

        [HttpPost, ActionName("DisableAccount")]
        [ValidateAntiForgeryToken]
        public ActionResult DisableAccountConfirmed(int id)
        {
            NHANVIEN nhanVien = db.NHANVIENs.Find(id);
            if (nhanVien == null)
            {
                return HttpNotFound();
            }

            nhanVien.TrangThai = false; // Assuming there is a property 'TrangThai' in the NHANVIEN model to indicate if the account is active or not
            db.SaveChanges();

            return RedirectToAction("Index");
        }
    }
}
