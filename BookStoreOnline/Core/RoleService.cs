using System.Collections.Generic;
using System.Linq;
using BookStoreOnline.Models;

namespace BookStoreOnline.Core
{
    public class RoleService
    {
        private readonly NhaSachEntities3 _db;

        public RoleService(NhaSachEntities3 db)
        {
            _db = db;
        }

        public void EnsureRoleTableExists()
        {
            _db.Database.ExecuteSqlCommand(@"
                IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'VAITRO')
                BEGIN
                    CREATE TABLE [dbo].[VAITRO](
                        [MaVaiTro] [int] NOT NULL,
                        [TenVaiTro] [nvarchar](100) NOT NULL,
                        [MoTa] [nvarchar](255) NULL,
                        [LoaiTaiKhoan] [nvarchar](20) NOT NULL,
                        CONSTRAINT [PK_VAITRO] PRIMARY KEY CLUSTERED ([MaVaiTro] ASC)
                    )
                END");

            SeedAndMigrateRoles();
            RepairQuyenColumnSchema();
            FixKhachHangMaVaiTroColumn();
            NormalizeStaffRoles();
            EnsureSampleShipperAccount();
        }

        private void SeedAndMigrateRoles()
        {
            UpsertRole(1, "Admin", "Quản trị toàn bộ hệ thống", "NhanVien");
            UpsertRole(2, "User", "Khách hàng mua sách online", "KhachHang");
            UpsertRole(3, "Shipper", "Giao hàng cho khách", "NhanVien");

            var hasLegacyRoles = _db.Database.SqlQuery<int>(
                "SELECT COUNT(*) FROM VAITRO WHERE MaVaiTro > 3").FirstOrDefault() > 0;

            if (hasLegacyRoles)
            {
                if (TableExists("NHANVIEN") && ColumnExists("NHANVIEN", "Quyen"))
                {
                    _db.Database.ExecuteSqlCommand("UPDATE NHANVIEN SET Quyen = 103 WHERE Quyen = 4");
                    _db.Database.ExecuteSqlCommand("UPDATE NHANVIEN SET Quyen = 1 WHERE Quyen IN (2) OR (Quyen = 3 AND Email NOT LIKE '%shipper%')");
                    _db.Database.ExecuteSqlCommand("UPDATE NHANVIEN SET Quyen = 3 WHERE Quyen = 103");
                }

                if (TableExists("KHACHHANG"))
                {
                    _db.Database.ExecuteSqlCommand(
                        "UPDATE KHACHHANG SET MaVaiTro = 2 WHERE MaVaiTro = 5 OR MaVaiTro NOT IN (1, 2, 3)");
                }

                _db.Database.ExecuteSqlCommand("DELETE FROM VAITRO WHERE MaVaiTro NOT IN (1, 2, 3)");
            }

            var count = _db.Database.SqlQuery<int>("SELECT COUNT(*) FROM VAITRO").FirstOrDefault();
            if (count == 0)
            {
                _db.Database.ExecuteSqlCommand(@"
                    INSERT INTO VAITRO (MaVaiTro, TenVaiTro, MoTa, LoaiTaiKhoan) VALUES
                    (1, N'Admin', N'Quản trị toàn bộ hệ thống', N'NhanVien'),
                    (2, N'User', N'Khách hàng mua sách online', N'KhachHang'),
                    (3, N'Shipper', N'Giao hàng cho khách', N'NhanVien')");
            }
        }

        /// <summary>
        /// Sửa schema Quyen an toàn, xử lý trạng thái migration bị dở dang (Quyen_New).
        /// </summary>
        private void RepairQuyenColumnSchema()
        {
            if (!TableExists("NHANVIEN")) return;

            var hasQuyen = ColumnExists("NHANVIEN", "Quyen");
            var hasQuyenNew = ColumnExists("NHANVIEN", "Quyen_New");

            // Trường hợp migration cũ bị dở: chỉ còn Quyen_New
            if (!hasQuyen && hasQuyenNew)
            {
                _db.Database.ExecuteSqlCommand("EXEC sp_rename 'NHANVIEN.Quyen_New', 'Quyen', 'COLUMN'");
                hasQuyen = true;
                hasQuyenNew = false;
            }

            // Trường hợp cả Quyen (string) và Quyen_New tồn tại cùng lúc
            if (hasQuyen && hasQuyenNew)
            {
                DropForeignKeyIfExists("FK_NHANVIEN_VAITRO");
                _db.Database.ExecuteSqlCommand(@"
                    UPDATE NHANVIEN SET Quyen_New =
                        CASE
                            WHEN Quyen_New IS NOT NULL THEN Quyen_New
                            WHEN TRY_CONVERT(INT, Quyen) IS NOT NULL THEN TRY_CONVERT(INT, Quyen)
                            WHEN LOWER(LTRIM(RTRIM(CONVERT(NVARCHAR(100), Quyen)))) LIKE N'%shipper%' THEN 3
                            ELSE 1
                        END");
                _db.Database.ExecuteSqlCommand("ALTER TABLE NHANVIEN DROP COLUMN Quyen");
                _db.Database.ExecuteSqlCommand("EXEC sp_rename 'NHANVIEN.Quyen_New', 'Quyen', 'COLUMN'");
                _db.Database.ExecuteSqlCommand("ALTER TABLE NHANVIEN ALTER COLUMN Quyen INT NOT NULL");
                NormalizeQuyenValues();
                return;
            }

            // Không có cột Quyen nào
            if (!hasQuyen)
            {
                _db.Database.ExecuteSqlCommand(
                    "ALTER TABLE NHANVIEN ADD Quyen INT NOT NULL CONSTRAINT DF_NHANVIEN_Quyen DEFAULT (1)");
                return;
            }

            // Quyen là kiểu chuỗi -> chuyển sang int từng bước
            if (IsStringColumn("NHANVIEN", "Quyen"))
            {
                DropForeignKeyIfExists("FK_NHANVIEN_VAITRO");

                if (!ColumnExists("NHANVIEN", "Quyen_New"))
                {
                    _db.Database.ExecuteSqlCommand("ALTER TABLE NHANVIEN ADD Quyen_New INT NULL");
                }

                _db.Database.ExecuteSqlCommand(@"
                    UPDATE NHANVIEN SET Quyen_New =
                        CASE
                            WHEN TRY_CONVERT(INT, Quyen) IS NOT NULL THEN TRY_CONVERT(INT, Quyen)
                            WHEN LOWER(LTRIM(RTRIM(CONVERT(NVARCHAR(100), Quyen)))) LIKE N'%admin%' THEN 1
                            WHEN LOWER(LTRIM(RTRIM(CONVERT(NVARCHAR(100), Quyen)))) LIKE N'%shipper%' THEN 3
                            ELSE 1
                        END
                    WHERE Quyen_New IS NULL");

                _db.Database.ExecuteSqlCommand("ALTER TABLE NHANVIEN DROP COLUMN Quyen");
                _db.Database.ExecuteSqlCommand("EXEC sp_rename 'NHANVIEN.Quyen_New', 'Quyen', 'COLUMN'");
                _db.Database.ExecuteSqlCommand("ALTER TABLE NHANVIEN ALTER COLUMN Quyen INT NOT NULL");
            }

            NormalizeQuyenValues();
        }

        private void NormalizeQuyenValues()
        {
            if (!ColumnExists("NHANVIEN", "Quyen")) return;

            _db.Database.ExecuteSqlCommand(@"
                UPDATE NHANVIEN SET Quyen = 3 WHERE Quyen = 4 OR Email LIKE '%shipper%';
                UPDATE NHANVIEN SET Quyen = 1 WHERE Quyen NOT IN (1, 3);");
        }

        private void FixKhachHangMaVaiTroColumn()
        {
            if (!TableExists("KHACHHANG")) return;

            if (!ColumnExists("KHACHHANG", "MaVaiTro"))
            {
                _db.Database.ExecuteSqlCommand(
                    "ALTER TABLE KHACHHANG ADD MaVaiTro INT NOT NULL CONSTRAINT DF_KHACHHANG_MaVaiTro DEFAULT (2)");
            }

            _db.Database.ExecuteSqlCommand(
                "UPDATE KHACHHANG SET MaVaiTro = 2 WHERE MaVaiTro IS NULL OR MaVaiTro = 0 OR MaVaiTro = 5");
        }

        private void NormalizeStaffRoles()
        {
            if (!ColumnExists("NHANVIEN", "Quyen")) return;

            _db.Database.ExecuteSqlCommand(@"
                UPDATE NHANVIEN SET Quyen = 3
                WHERE Email LIKE '%shipper%' OR Ten LIKE N'%shipper%' OR Ten LIKE N'%Shipper%'");

            _db.Database.ExecuteSqlCommand(@"
                UPDATE NHANVIEN SET Quyen = 1
                WHERE Quyen NOT IN (1, 3)");
        }

        private void EnsureSampleShipperAccount()
        {
            if (!ColumnExists("NHANVIEN", "Quyen")) return;

            var shipperExists = _db.Database.SqlQuery<int>(
                "SELECT COUNT(*) FROM NHANVIEN WHERE Email = @p0", "shipper@nhasach.com").FirstOrDefault() > 0;

            if (!shipperExists)
            {
                _db.Database.ExecuteSqlCommand(@"
                    INSERT INTO NHANVIEN (Email, MatKhau, Quyen, Ten, NgayTao, TrangThai)
                    VALUES (N'shipper@nhasach.com', N'shipper123', 3, N'Nguyễn Văn Shipper', GETDATE(), 1)");
            }
            else
            {
                _db.Database.ExecuteSqlCommand(@"
                    UPDATE NHANVIEN SET Quyen = 3, TrangThai = 1, MatKhau = N'shipper123'
                    WHERE Email = @p0", "shipper@nhasach.com");
            }
        }

        private void DropForeignKeyIfExists(string constraintName)
        {
            _db.Database.ExecuteSqlCommand(string.Format(@"
                IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = '{0}')
                    ALTER TABLE NHANVIEN DROP CONSTRAINT [{0}];", constraintName));
        }

        private bool TableExists(string tableName)
        {
            return _db.Database.SqlQuery<int>(
                "SELECT COUNT(*) FROM sys.tables WHERE name = @p0", tableName).FirstOrDefault() > 0;
        }

        private bool ColumnExists(string tableName, string columnName)
        {
            return _db.Database.SqlQuery<int>(@"
                SELECT COUNT(*) FROM sys.columns
                WHERE object_id = OBJECT_ID(@p0) AND name = @p1",
                tableName, columnName).FirstOrDefault() > 0;
        }

        private bool IsStringColumn(string tableName, string columnName)
        {
            return _db.Database.SqlQuery<int>(@"
                SELECT COUNT(*)
                FROM sys.columns c
                INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
                WHERE c.object_id = OBJECT_ID(@p0) AND c.name = @p1
                  AND t.name IN ('varchar', 'nvarchar', 'char', 'nchar')",
                tableName, columnName).FirstOrDefault() > 0;
        }

        private void UpsertRole(int id, string name, string moTa, string loai)
        {
            var exists = _db.Database.SqlQuery<int>(
                "SELECT COUNT(*) FROM VAITRO WHERE MaVaiTro = @p0", id).FirstOrDefault() > 0;
            if (exists)
            {
                _db.Database.ExecuteSqlCommand(
                    "UPDATE VAITRO SET TenVaiTro = @p0, MoTa = @p1, LoaiTaiKhoan = @p2 WHERE MaVaiTro = @p3",
                    name, moTa, loai, id);
            }
            else
            {
                _db.Database.ExecuteSqlCommand(
                    "INSERT INTO VAITRO (MaVaiTro, TenVaiTro, MoTa, LoaiTaiKhoan) VALUES (@p0, @p1, @p2, @p3)",
                    id, name, moTa, loai);
            }
        }

        public List<VAITRO> GetAllRoles()
        {
            return _db.Database.SqlQuery<VAITRO>(
                "SELECT MaVaiTro, TenVaiTro, MoTa, LoaiTaiKhoan FROM VAITRO ORDER BY MaVaiTro").ToList();
        }

        public List<VAITRO> GetStaffRoles()
        {
            return _db.Database.SqlQuery<VAITRO>(
                "SELECT MaVaiTro, TenVaiTro, MoTa, LoaiTaiKhoan FROM VAITRO WHERE LoaiTaiKhoan = N'NhanVien' ORDER BY MaVaiTro").ToList();
        }

        public VAITRO GetById(int maVaiTro)
        {
            return _db.Database.SqlQuery<VAITRO>(
                "SELECT MaVaiTro, TenVaiTro, MoTa, LoaiTaiKhoan FROM VAITRO WHERE MaVaiTro = @p0", maVaiTro).FirstOrDefault();
        }

        public string GetRoleName(int maVaiTro)
        {
            var role = GetById(maVaiTro);
            return role?.TenVaiTro ?? "Không xác định";
        }

        public bool IsValidStaffRole(int maVaiTro)
        {
            return maVaiTro == 1 || maVaiTro == 3;
        }

        public void AssignCustomerRole(int maKh, int maVaiTro = 2)
        {
            _db.Database.ExecuteSqlCommand(
                "UPDATE KHACHHANG SET MaVaiTro = @p0 WHERE MaKH = @p1", maVaiTro, maKh);
        }

        public int GetCustomerRole(int maKh)
        {
            return _db.Database.SqlQuery<int>(
                "SELECT ISNULL(MaVaiTro, 2) FROM KHACHHANG WHERE MaKH = @p0", maKh).FirstOrDefault();
        }
    }
}
