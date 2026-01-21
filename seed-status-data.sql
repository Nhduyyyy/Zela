-- Script để insert dữ liệu cố định vào bảng Statuses
-- Chạy script này sau khi đã chạy migrations
-- StatusId sẽ tự động tăng (IDENTITY)

USE [Zela_FinalV2.0];
GO

-- Kiểm tra xem đã có dữ liệu chưa, nếu có thì xóa các bản ghi cố định
IF EXISTS (SELECT 1 FROM [Statuses] WHERE [StatuName] IN (N'Pending', N'Accepted', N'Rejected'))
BEGIN
    PRINT 'Đã có dữ liệu Status, đang xóa dữ liệu cũ...';
    DELETE FROM [Statuses] WHERE [StatuName] IN (N'Pending', N'Accepted', N'Rejected');
END
GO

-- Insert 3 bản ghi cố định (StatusId sẽ tự động tăng)
INSERT INTO [Statuses] ([StatuName], [CreatedAt], [Describe])
VALUES 
    (N'Pending', '2025-06-06 11:19:55.0000000', N'Chờ phản hồi'),
    (N'Accepted', '2025-06-06 11:20:30.0000000', N'Đã chấp nhận'),
    (N'Rejected', '2025-06-06 11:21:01.0000000', N'Đã từ chối');
GO

-- Kiểm tra kết quả
SELECT [StatusId], [StatuName], [CreatedAt], [Describe] 
FROM [Statuses] 
ORDER BY [StatusId];
GO

PRINT '✅ Đã insert thành công 3 bản ghi Status!';
GO

