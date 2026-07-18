USE [FlexFitPaymentDb];
GO

-- Xóa dữ liệu cũ nếu muốn reset danh sách gói (tùy chọn, comment lại nếu không muốn xóa)
-- DELETE FROM [CreditPackages];
-- GO

-- Seed Data cho CreditPackages
IF NOT EXISTS(SELECT * FROM [CreditPackages] WHERE [PackageId] = '11111111-1111-1111-1111-111111111111')
BEGIN
    INSERT INTO [CreditPackages] ([PackageId], [PackageName], [CreditAmount], [BonusCredit], [Price], [Description], [IsPopular], [IsActive], [CreatedAt])
    VALUES 
    ('11111111-1111-1111-1111-111111111111', N'Gói Cơ Bản', 100, 0, 100000.00, N'Gói 100 Credit cơ bản', 0, 1, GETUTCDATE());
END;

IF NOT EXISTS(SELECT * FROM [CreditPackages] WHERE [PackageId] = '22222222-2222-2222-2222-222222222222')
BEGIN
    INSERT INTO [CreditPackages] ([PackageId], [PackageName], [CreditAmount], [BonusCredit], [Price], [Description], [IsPopular], [IsActive], [CreatedAt])
    VALUES 
    ('22222222-2222-2222-2222-222222222222', N'Gói Phổ Biến', 500, 50, 500000.00, N'Gói 500 Credit + 50 Credit thưởng', 1, 1, GETUTCDATE());
END;

IF NOT EXISTS(SELECT * FROM [CreditPackages] WHERE [PackageId] = '33333333-3333-3333-3333-333333333333')
BEGIN
    INSERT INTO [CreditPackages] ([PackageId], [PackageName], [CreditAmount], [BonusCredit], [Price], [Description], [IsPopular], [IsActive], [CreatedAt])
    VALUES 
    ('33333333-3333-3333-3333-333333333333', N'Gói Cao Cấp', 1000, 150, 1000000.00, N'Gói 1000 Credit + 150 Credit thưởng', 0, 1, GETUTCDATE());
END;

-- Thêm các gói đặc biệt cho demo
IF NOT EXISTS(SELECT * FROM [CreditPackages] WHERE [PackageId] = '44444444-4444-4444-4444-444444444444')
BEGIN
    INSERT INTO [CreditPackages] ([PackageId], [PackageName], [CreditAmount], [BonusCredit], [Price], [Description], [IsPopular], [IsActive], [CreatedAt])
    VALUES 
    ('44444444-4444-4444-4444-444444444444', N'Gói VIP', 5000, 1000, 5000000.00, N'Gói VIP đặc biệt cho người dùng trung thành', 0, 1, GETUTCDATE());
END;

GO
