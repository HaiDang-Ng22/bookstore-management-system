-- Migration: Add VIP Customer Classification
-- This script adds VIP/Regular customer classification to the KHACHHANG table

USE [NhaSach]
GO

-- Add new columns to KHACHHANG table
ALTER TABLE [dbo].[KHACHHANG]
ADD 
    [LoaiKhachHang] [nvarchar](50) DEFAULT 'Regular' NULL,
    [TongChiTieu] [bigint] DEFAULT 0 NULL,
    [NgayCapNhatVIP] [datetime] NULL;
GO

-- Add check constraint to ensure LoaiKhachHang has valid values
ALTER TABLE [dbo].[KHACHHANG]
ADD CONSTRAINT [CK_LoaiKhachHang_Values] 
CHECK ([LoaiKhachHang] IN ('Regular', 'VIP'));
GO

-- Create index for quick lookup by customer type
CREATE INDEX [IX_KHACHHANG_LoaiKhachHang] 
ON [dbo].[KHACHHANG]([LoaiKhachHang]);
GO

-- Update existing customers to have LoaiKhachHang = 'Regular'
UPDATE [dbo].[KHACHHANG]
SET [LoaiKhachHang] = 'Regular',
    [TongChiTieu] = 0
WHERE [LoaiKhachHang] IS NULL;
GO

-- Create a stored procedure to calculate total spending for a customer
CREATE OR ALTER PROCEDURE [dbo].[sp_UpdateCustomerType]
    @MaKH INT
AS
BEGIN
    DECLARE @TongChiTieu BIGINT;
    
    -- Calculate total spending for the customer (sum of all completed orders)
    SELECT @TongChiTieu = ISNULL(SUM(TongTien), 0)
    FROM [dbo].[DONHANG]
    WHERE [ID] = @MaKH 
      AND [TrangThai] IN (2, 3) -- Only count confirmed/completed orders
      AND [TrangThaiThanhToan] IN (1, 2); -- Only count paid orders
    
    -- Update customer record
    UPDATE [dbo].[KHACHHANG]
    SET [TongChiTieu] = @TongChiTieu,
        [LoaiKhachHang] = CASE 
            WHEN @TongChiTieu >= 5000000 THEN 'VIP'  -- VIP if spending >= 5,000,000
            ELSE 'Regular'
        END,
        [NgayCapNhatVIP] = GETDATE()
    WHERE [MaKH] = @MaKH;
END;
GO

-- Create a stored procedure to get VIP statistics
CREATE OR ALTER PROCEDURE [dbo].[sp_GetVIPStatistics]
AS
BEGIN
    SELECT 
        COUNT(*) as TotalCustomers,
        SUM(CASE WHEN [LoaiKhachHang] = 'VIP' THEN 1 ELSE 0 END) as VIPCustomers,
        SUM(CASE WHEN [LoaiKhachHang] = 'Regular' THEN 1 ELSE 0 END) as RegularCustomers,
        AVG([TongChiTieu]) as AverageSpending,
        MAX([TongChiTieu]) as MaximumSpending
    FROM [dbo].[KHACHHANG]
    WHERE [TrangThai] = 1;
END;
GO

PRINT 'VIP Customer Classification has been successfully added to the database!';
