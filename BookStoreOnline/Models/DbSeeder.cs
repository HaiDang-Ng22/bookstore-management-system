using System;
using System.Linq;
using System.Security.Cryptography;
using System.Text;
using BookStoreOnline.Models;

namespace BookStoreOnline.Models
{
    public static class DbSeeder
    {
        private static string HashPassword(string password)
        {
            using (var sha256 = SHA256.Create())
            {
                var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
                return Convert.ToBase64String(bytes);
            }
        }

        public static void Seed()
        {
            using (var db = new NhaSachEntities3())
            {
                // 0. Ensure GIOHANG table exists
                db.Database.ExecuteSqlCommand(@"
                    IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[GIOHANG]') AND type in (N'U'))
                    BEGIN
                        CREATE TABLE [dbo].[GIOHANG] (
                            [MaKH] INT NOT NULL,
                            [MaSanPham] INT NOT NULL,
                            [SoLuong] INT NOT NULL DEFAULT 1,
                            [NgayTao] DATETIME NULL DEFAULT GETDATE(),
                            CONSTRAINT [PK_GIOHANG] PRIMARY KEY CLUSTERED ([MaKH] ASC, [MaSanPham] ASC),
                            CONSTRAINT [FK_GIOHANG_KHACHHANG] FOREIGN KEY ([MaKH]) REFERENCES [dbo].[KHACHHANG] ([MaKH]) ON DELETE CASCADE,
                            CONSTRAINT [FK_GIOHANG_SANPHAM] FOREIGN KEY ([MaSanPham]) REFERENCES [dbo].[SANPHAM] ([MaSanPham]) ON DELETE CASCADE
                        )
                    END
                ");

                // 1. Seed NHANVIEN (Admin, Managers)
                if (!db.NHANVIENs.Any())
                {
                    db.NHANVIENs.Add(new NHANVIEN
                    {
                        Ten = "Administrator",
                        Email = "admin@bookstore.com",
                        MatKhau = "admin", // Plain text in original code logic
                        Quyen = 1, // Administrator role
                        NgayTao = DateTime.Now,
                        TrangThai = true
                    });
                    db.NHANVIENs.Add(new NHANVIEN
                    {
                        Ten = "Quản Trị Viên Backup",
                        Email = "admin@gmail.com",
                        MatKhau = "123456", // Plain text
                        Quyen = 1, // Administrator role
                        NgayTao = DateTime.Now,
                        TrangThai = true
                    });
                    db.SaveChanges();
                }

                // 2. Seed KHACHHANG (Customers)
                if (!db.KHACHHANGs.Any())
                {
                    var customer = new KHACHHANG
                    {
                        Ten = "Khách hàng mẫu",
                        Email = "customer@bookstore.com",
                        MatKhau = HashPassword("123456"),
                        SoDienThoai = "0908123456",
                        DiaChi = "123 Đường Sách, Quận 1, TP. HCM",
                        NgayTao = DateTime.Now,
                        TrangThai = true
                    };
                    db.KHACHHANGs.Add(customer);
                    db.SaveChanges();
                }

                // 3. Seed LOAI (Categories)
                if (!db.LOAIs.Any())
                {
                    db.LOAIs.Add(new LOAI { Tenloai = "Sách Văn Học" });
                    db.LOAIs.Add(new LOAI { Tenloai = "Sách Khoa Học" });
                    db.LOAIs.Add(new LOAI { Tenloai = "Truyện Tranh" });
                    db.LOAIs.Add(new LOAI { Tenloai = "Kinh Tế - Kinh Doanh" });
                    db.SaveChanges();
                }

                // 4. Seed SANPHAM (Products)
                if (!db.SANPHAMs.Any())
                {
                    var categories = db.LOAIs.ToList();
                    var vanHoc = categories.FirstOrDefault(c => c.Tenloai == "Sách Văn Học");
                    var khoaHoc = categories.FirstOrDefault(c => c.Tenloai == "Sách Khoa Học");
                    var truyenTranh = categories.FirstOrDefault(c => c.Tenloai == "Truyện Tranh");
                    var kinhTe = categories.FirstOrDefault(c => c.Tenloai == "Kinh Tế - Kinh Doanh");

                    var adminUser = db.NHANVIENs.FirstOrDefault();
                    int? adminId = adminUser != null ? (int?)adminUser.MaNV : null;

                    if (vanHoc != null)
                    {
                        db.SANPHAMs.Add(new SANPHAM
                        {
                            TenSanPham = "Dế Mèn Phiêu Lưu Ký",
                            Gia = 45000,
                            MoTa = "Tác phẩm văn học thiếu nhi nổi tiếng của nhà văn Tô Hoài.",
                            TacGia = "Tô Hoài",
                            Anh = "demen.jpg",
                            MaLoai = vanHoc.Maloai,
                            SoLuong = 50,
                            SoLuongBan = 0,
                            MaNVTao = adminId
                        });
                        db.SANPHAMs.Add(new SANPHAM
                        {
                            TenSanPham = "Số Đỏ",
                            Gia = 60000,
                            MoTa = "Tiểu thuyết trào phúng kinh điển của Vũ Trọng Phụng.",
                            TacGia = "Vũ Trọng Phụng",
                            Anh = "sodo.jpg",
                            MaLoai = vanHoc.Maloai,
                            SoLuong = 30,
                            SoLuongBan = 0,
                            MaNVTao = adminId
                        });
                    }

                    if (khoaHoc != null)
                    {
                        db.SANPHAMs.Add(new SANPHAM
                        {
                            TenSanPham = "Lược Sử Thời Gian",
                            Gia = 120000,
                            MoTa = "Cuốn sách khoa học vũ trụ kinh điển viết bởi Stephen Hawking.",
                            TacGia = "Stephen Hawking",
                            Anh = "luocsuthoigian.jpg",
                            MaLoai = khoaHoc.Maloai,
                            SoLuong = 20,
                            SoLuongBan = 0,
                            MaNVTao = adminId
                        });
                    }

                    if (truyenTranh != null)
                    {
                        db.SANPHAMs.Add(new SANPHAM
                        {
                            TenSanPham = "Doraemon Tập 1",
                            Gia = 25000,
                            MoTa = "Bộ truyện tranh chú mèo máy thông minh đến từ tương lai.",
                            TacGia = "Fujiko F. Fujio",
                            Anh = "doraemon1.jpg",
                            MaLoai = truyenTranh.Maloai,
                            SoLuong = 100,
                            SoLuongBan = 0,
                            MaNVTao = adminId
                        });
                    }

                    if (kinhTe != null)
                    {
                        db.SANPHAMs.Add(new SANPHAM
                        {
                            TenSanPham = "Nghĩ Giàu Làm Giàu",
                            Gia = 95000,
                            MoTa = "Cuốn sách dạy làm giàu bán chạy nhất mọi thời đại của Napoleon Hill.",
                            TacGia = "Napoleon Hill",
                            Anh = "nghigiaulamgiau.jpg",
                            MaLoai = kinhTe.Maloai,
                            SoLuong = 40,
                            SoLuongBan = 0,
                            MaNVTao = adminId
                        });
                    }

                    db.SaveChanges();
                }
            }
        }
    }
}
