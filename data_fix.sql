-- Script Data Fix cho bảng SellerSettlement do các version code cũ sinh ra dữ liệu lỗi
-- Yêu cầu: status PENDING thì bắt buộc heldAt, availableAt, releasedAt, holdReason phải là NULL
-- Chạy script này trên SQL Server (SSMS) đối với database CloneEbayDB

USE CloneEbayDB;
GO

BEGIN TRAN;

-- Cập nhật làm sạch các record bị dính HoldReason hoặc time khi mới PENDING
UPDATE SellerSettlement
SET holdReason = NULL,
    heldAt = NULL,
    availableAt = NULL,
    releasedAt = NULL
WHERE status = 'PENDING'
  AND (holdReason IS NOT NULL OR heldAt IS NOT NULL OR availableAt IS NOT NULL OR releasedAt IS NOT NULL);

PRINT 'Đã fix thành công các record SellerSettlement bị kẹt data rác ở trạng thái PENDING.';

COMMIT TRAN;
GO
