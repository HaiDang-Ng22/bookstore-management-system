-- Sửa lỗi Quyen_New và thêm tài khoản Shipper
-- Đổi USE [TênDatabase] cho đúng DB của bạn (vd: db54846 hoặc NhaSach)
USE [NhaSach]
GO

-- 1. Sửa migration bị dở dang
IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('NHANVIEN') AND name = 'Quyen_New')
   AND NOT EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('NHANVIEN') AND name = 'Quyen')
BEGIN
    EXEC sp_rename 'NHANVIEN.Quyen_New', 'Quyen', 'COLUMN'
END
GO

IF EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('NHANVIEN') AND name = 'Quyen_New')
   AND EXISTS (SELECT 1 FROM sys.columns WHERE object_id = OBJECT_ID('NHANVIEN') AND name = 'Quyen')
BEGIN
    UPDATE NHANVIEN SET Quyen_New = TRY_CONVERT(INT, Quyen) WHERE Quyen_New IS NULL
    ALTER TABLE NHANVIEN DROP COLUMN Quyen
    EXEC sp_rename 'NHANVIEN.Quyen_New', 'Quyen', 'COLUMN'
END
GO

-- 2. Đảm bảo 3 role
MERGE VAITRO AS target
USING (VALUES
    (1, N'Admin', N'Quản trị toàn bộ hệ thống', N'NhanVien'),
    (2, N'User', N'Khách hàng mua sách online', N'KhachHang'),
    (3, N'Shipper', N'Giao hàng cho khách', N'NhanVien')
) AS source (MaVaiTro, TenVaiTro, MoTa, LoaiTaiKhoan)
ON target.MaVaiTro = source.MaVaiTro
WHEN MATCHED THEN UPDATE SET TenVaiTro = source.TenVaiTro, MoTa = source.MoTa, LoaiTaiKhoan = source.LoaiTaiKhoan
WHEN NOT MATCHED THEN INSERT (MaVaiTro, TenVaiTro, MoTa, LoaiTaiKhoan) VALUES (source.MaVaiTro, source.TenVaiTro, source.MoTa, source.LoaiTaiKhoan);
GO

DELETE FROM VAITRO WHERE MaVaiTro NOT IN (1, 2, 3)
GO

-- 3. Thêm / cập nhật tài khoản Shipper
IF NOT EXISTS (SELECT 1 FROM NHANVIEN WHERE Email = 'shipper@nhasach.com')
BEGIN
    INSERT INTO NHANVIEN (Email, MatKhau, Quyen, Ten, NgayTao, TrangThai)
    VALUES (N'shipper@nhasach.com', N'shipper123', 3, N'Nguyễn Văn Shipper', GETDATE(), 1)
END
ELSE
BEGIN
    UPDATE NHANVIEN SET Quyen = 3, TrangThai = 1, MatKhau = N'shipper123'
    WHERE Email = 'shipper@nhasach.com'
END
GO

-- 4. Admin giữ Quyen = 1
UPDATE NHANVIEN SET Quyen = 1
WHERE Email NOT LIKE '%shipper%' AND Quyen <> 3
GO

SELECT * FROM VAITRO
SELECT MaNV, Email, MatKhau, Ten, Quyen FROM NHANVIEN
GO
