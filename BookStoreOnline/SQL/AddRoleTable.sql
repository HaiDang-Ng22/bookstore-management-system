-- Sửa lỗi đăng nhập: chuẩn hóa 3 role Admin / User / Shipper
USE [NhaSach]
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'VAITRO')
BEGIN
    CREATE TABLE [dbo].[VAITRO](
        [MaVaiTro] [int] NOT NULL,
        [TenVaiTro] [nvarchar](100) NOT NULL,
        [MoTa] [nvarchar](255) NULL,
        [LoaiTaiKhoan] [nvarchar](20) NOT NULL,
        CONSTRAINT [PK_VAITRO] PRIMARY KEY CLUSTERED ([MaVaiTro] ASC)
    )
END
GO

MERGE VAITRO AS target
USING (VALUES
    (1, N'Admin', N'Quản trị toàn bộ hệ thống', N'NhanVien'),
    (2, N'User', N'Khách hàng mua sách online', N'KhachHang'),
    (3, N'Shipper', N'Giao hàng cho khách', N'NhanVien')
) AS source (MaVaiTro, TenVaiTro, MoTa, LoaiTaiKhoan)
ON target.MaVaiTro = source.MaVaiTro
WHEN MATCHED THEN
    UPDATE SET TenVaiTro = source.TenVaiTro, MoTa = source.MoTa, LoaiTaiKhoan = source.LoaiTaiKhoan
WHEN NOT MATCHED THEN
    INSERT (MaVaiTro, TenVaiTro, MoTa, LoaiTaiKhoan) VALUES (source.MaVaiTro, source.TenVaiTro, source.MoTa, source.LoaiTaiKhoan);
GO

DELETE FROM VAITRO WHERE MaVaiTro NOT IN (1, 2, 3)
GO

-- Sửa cột Quyen nếu đang là kiểu chuỗi (gây lỗi EF Int32)
IF EXISTS (
    SELECT 1 FROM sys.columns c
    INNER JOIN sys.types t ON c.user_type_id = t.user_type_id
    WHERE c.object_id = OBJECT_ID('NHANVIEN') AND c.name = 'Quyen'
      AND t.name IN ('varchar', 'nvarchar', 'char', 'nchar')
)
BEGIN
    IF EXISTS (SELECT 1 FROM sys.foreign_keys WHERE name = 'FK_NHANVIEN_VAITRO')
        ALTER TABLE NHANVIEN DROP CONSTRAINT FK_NHANVIEN_VAITRO

    ALTER TABLE NHANVIEN ADD Quyen_New INT NULL

    UPDATE NHANVIEN SET Quyen_New =
        CASE
            WHEN TRY_CONVERT(INT, Quyen) IS NOT NULL THEN TRY_CONVERT(INT, Quyen)
            WHEN LOWER(LTRIM(RTRIM(CONVERT(NVARCHAR(100), Quyen)))) LIKE N'%admin%' THEN 1
            WHEN LOWER(LTRIM(RTRIM(CONVERT(NVARCHAR(100), Quyen)))) LIKE N'%shipper%' THEN 3
            ELSE 1
        END

    ALTER TABLE NHANVIEN DROP COLUMN Quyen
    EXEC sp_rename 'NHANVIEN.Quyen_New', 'Quyen', 'COLUMN'
    ALTER TABLE NHANVIEN ALTER COLUMN Quyen INT NOT NULL
END
GO

-- Chuẩn hóa giá trị Quyen (1=Admin, 3=Shipper)
UPDATE NHANVIEN SET Quyen = 3 WHERE Quyen = 4 OR Email LIKE '%shipper%'
UPDATE NHANVIEN SET Quyen = 1 WHERE Quyen NOT IN (1, 3)
GO

IF NOT EXISTS (SELECT * FROM sys.columns WHERE object_id = OBJECT_ID('KHACHHANG') AND name = 'MaVaiTro')
BEGIN
    ALTER TABLE KHACHHANG ADD MaVaiTro INT NOT NULL CONSTRAINT DF_KHACHHANG_MaVaiTro DEFAULT (2)
END
GO

UPDATE KHACHHANG SET MaVaiTro = 2 WHERE MaVaiTro IS NULL OR MaVaiTro = 0 OR MaVaiTro = 5 OR MaVaiTro NOT IN (1, 2, 3)
GO

IF NOT EXISTS (SELECT * FROM NHANVIEN WHERE Email = 'shipper@nhasach.com')
BEGIN
    INSERT INTO NHANVIEN (Email, MatKhau, Quyen, Ten, NgayTao, TrangThai)
    VALUES (N'shipper@nhasach.com', N'shipper123', 3, N'Nguyễn Văn Shipper', GETDATE(), 1)
END
ELSE
BEGIN
    UPDATE NHANVIEN SET Quyen = 3 WHERE Email = 'shipper@nhasach.com'
END
GO

PRINT N'Hoàn tất! 3 role: 1=Admin, 2=User (KHACHHANG), 3=Shipper (NHANVIEN)'
GO
