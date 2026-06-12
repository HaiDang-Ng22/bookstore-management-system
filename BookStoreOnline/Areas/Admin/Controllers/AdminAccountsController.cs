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
    [AdminAuthorize(AdminRole.Administrator)]
    public class AdminAccountsController : Controller
    {
        private NhaSachEntities3 db = new NhaSachEntities3();

        // GET: Admin/NHANVIENs
        public ActionResult Index()
        {
            return View(db.NHANVIENs.ToList());
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
            var roles = from AdminRole e in Enum.GetValues(typeof(AdminRole))
                        select new { Id = (int)e, Name = e.GetDescription() };

            ViewBag.Role = new SelectList(roles, "Id", "Name");
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

            var roles = from AdminRole e in Enum.GetValues(typeof(AdminRole))
                        select new { Id = (int)e, Name = e.GetDescription() };
            ViewBag.Role = new SelectList(roles, "Id", "Name");

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
            var roles = from AdminRole e in Enum.GetValues(typeof(AdminRole))
                        select new { Id = (int)e, Name = e.GetDescription() };

            ViewBag.Role = new SelectList(roles, "Id", "Name");
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
                //NHANVIEN admAccount = db.NHANVIENs.Find(nhanVien.MaNV);
                //admAccount.Ten = nhanVien.Ten;
                //admAccount.Quyen = nhanVien.Quyen;
                //admAccount.Email = nhanVien.Email;
                //admAccount.TrangThai= nhanVien.TrangThai;
                //db.Entry(admAccount).State = EntityState.Modified;
                //db.SaveChanges();
                //return RedirectToAction("Index");
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

            var roles = from AdminRole e in Enum.GetValues(typeof(AdminRole))
                        select new { Id = (int)e, Name = e.GetDescription() };
            ViewBag.Role = new SelectList(roles, "Id", "Name");

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
