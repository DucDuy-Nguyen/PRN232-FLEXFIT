-- ============================================================================
-- FLEXFIT MULTI-DATABASE SEED SCRIPT (FOR MICROSERVICES ARCHITECTURE)
-- Database per Service:
-- 1. FlexFitIdentityDb (Users, Roles, UserRoles, MemberProfiles)
-- 2. FlexFitCatalogDb  (Gyms, Branches, Categories, GymAmenities, Sessions, Classes)
-- 3. FlexFitBookingDb  (GymBookings, ClassBookings, CheckInLogs)
-- 4. FlexFitPaymentDb  (CreditPackages, UserCredits, CreditTransactions, Payments)
-- 5. FlexFitDB         (Engagement/Reviews, Favorites, Notifications, Histories)
-- Mật khẩu mặc định tất cả tài khoản: 123456aA@
-- ============================================================================

SET NOCOUNT ON;
SET QUOTED_IDENTIFIER ON;
SET ANSI_NULLS ON;

-- ----------------------------------------------------------------------------
-- 1. SEED FOR FlexFitIdentityDb
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'FlexFitIdentityDb')
BEGIN
    CREATE DATABASE FlexFitIdentityDb;
END;
GO

USE FlexFitIdentityDb;
GO

BEGIN TRANSACTION;
BEGIN TRY
    IF OBJECT_ID('Roles', 'U') IS NOT NULL
    BEGIN
        DELETE FROM MemberProfiles;
        DELETE FROM UserRoles;
        DELETE FROM Users;
        DELETE FROM Roles;

        DECLARE @RoleId_Admin UNIQUEIDENTIFIER = 'D7F8A7CF-CF06-4447-9204-CCF3B2EE0001';
        DECLARE @RoleId_Member UNIQUEIDENTIFIER = 'D7F8A7CF-CF06-4447-9204-CCF3B2EE0002';
        DECLARE @RoleId_GymPartner UNIQUEIDENTIFIER = 'D7F8A7CF-CF06-4447-9204-CCF3B2EE0003';
        DECLARE @RoleId_Staff UNIQUEIDENTIFIER = 'D7F8A7CF-CF06-4447-9204-CCF3B2EE0004';

        INSERT INTO Roles (RoleId, RoleName, Description, CreatedAt)
        VALUES
        (@RoleId_Admin, N'Admin', N'System Administrator', GETUTCDATE()),
        (@RoleId_Member, N'Member', N'Gym Member', GETUTCDATE()),
        (@RoleId_GymPartner, N'GymPartner', N'Gym Owner / Partner', GETUTCDATE()),
        (@RoleId_Staff, N'Staff', N'Gym Staff member', GETUTCDATE());

        DECLARE @DefaultPasswordHash NVARCHAR(MAX) = N'jNLoDEgMpxaYbAYi0zKE8Q==.7SBRcftphBGpmMCtkJg2H/QzaBV3lMuwwQRo6m/3WTA=';

        DECLARE @UserId_Admin UNIQUEIDENTIFIER = 'A1000000-0000-0000-0000-000000000001';
        DECLARE @UserId_OwnerFitZone UNIQUEIDENTIFIER = 'A2000000-0000-0000-0000-000000000001';
        DECLARE @UserId_OwnerCali UNIQUEIDENTIFIER = 'A2000000-0000-0000-0000-000000000002';
        DECLARE @UserId_StaffQ1 UNIQUEIDENTIFIER = 'A3000000-0000-0000-0000-000000000001';
        DECLARE @UserId_StaffQ7 UNIQUEIDENTIFIER = 'A3000000-0000-0000-0000-000000000002';
        DECLARE @UserId_Member1 UNIQUEIDENTIFIER = 'A4000000-0000-0000-0000-000000000001';
        DECLARE @UserId_Member2 UNIQUEIDENTIFIER = 'A4000000-0000-0000-0000-000000000002';
        DECLARE @UserId_Member3 UNIQUEIDENTIFIER = 'A4000000-0000-0000-0000-000000000003';

        INSERT INTO Users (UserId, FullName, Email, PasswordHash, PhoneNumber, DateOfBirth, AvatarUrl, IsEmailVerified, IsActive, CreatedAt)
        VALUES
        (@UserId_Admin, N'System Admin', N'admin@flexfit.com', @DefaultPasswordHash, N'0901000001', '1990-01-01', N'https://images.unsplash.com/photo-1534528741775-53994a69daeb', 1, 1, GETUTCDATE()),
        (@UserId_OwnerFitZone, N'FitZone Owner', N'owner.fitzone@flexfit.com', @DefaultPasswordHash, N'0902000001', '1985-05-15', N'https://images.unsplash.com/photo-1507003211169-0a1dd7228f2d', 1, 1, GETUTCDATE()),
        (@UserId_OwnerCali, N'California Owner', N'owner.cali@flexfit.com', @DefaultPasswordHash, N'0902000002', '1988-08-20', N'https://images.unsplash.com/photo-1500648767791-00dcc994a43e', 1, 1, GETUTCDATE()),
        (@UserId_StaffQ1, N'Staff Quận 1', N'staff.district1@flexfit.com', @DefaultPasswordHash, N'0903000001', '1995-03-10', N'https://images.unsplash.com/photo-1494790108377-be9c29b29330', 1, 1, GETUTCDATE()),
        (@UserId_StaffQ7, N'Staff Quận 7', N'staff.district7@flexfit.com', @DefaultPasswordHash, N'0903000002', '1997-11-25', N'https://images.unsplash.com/photo-1438761681033-6461ffad8d80', 1, 1, GETUTCDATE()),
        (@UserId_Member1, N'Nguyễn Văn A', N'user1@gmail.com', @DefaultPasswordHash, N'0904000001', '1998-02-14', N'https://images.unsplash.com/photo-1539571696357-5a69c17a67c6', 1, 1, GETUTCDATE()),
        (@UserId_Member2, N'Trần Thị B', N'user2@gmail.com', @DefaultPasswordHash, N'0904000002', '2000-06-30', N'https://images.unsplash.com/photo-1517841905240-472988babdf9', 1, 1, GETUTCDATE()),
        (@UserId_Member3, N'Lê Hoàng C', N'user3@gmail.com', @DefaultPasswordHash, N'0904000003', '1993-09-12', N'https://images.unsplash.com/photo-1522075469751-3a6694fb2f61', 1, 1, GETUTCDATE());

        INSERT INTO UserRoles (UserId, RoleId, AssignedAt)
        VALUES
        (@UserId_Admin, @RoleId_Admin, GETUTCDATE()),
        (@UserId_OwnerFitZone, @RoleId_GymPartner, GETUTCDATE()),
        (@UserId_OwnerCali, @RoleId_GymPartner, GETUTCDATE()),
        (@UserId_StaffQ1, @RoleId_Staff, GETUTCDATE()),
        (@UserId_StaffQ7, @RoleId_Staff, GETUTCDATE()),
        (@UserId_Member1, @RoleId_Member, GETUTCDATE()),
        (@UserId_Member2, @RoleId_Member, GETUTCDATE()),
        (@UserId_Member3, @RoleId_Member, GETUTCDATE());

        INSERT INTO MemberProfiles (MemberProfileId, UserId, Gender, HeightCm, WeightKg, FitnessGoal, ActivityLevel, PreferredWorkoutTime, Bio)
        VALUES
        (NEWID(), @UserId_Member1, N'Male', 175.0, 70.0, N'Tăng cơ & Giảm mỡ', N'Moderate', N'Morning', N'Yêu thích tập Gym và Boxing'),
        (NEWID(), @UserId_Member2, N'Female', 162.0, 52.0, N'Tăng sự dẻo dai & Pilates', N'Light', N'Evening', N'Thích các lớp Yoga và Pilates thư giãn'),
        (NEWID(), @UserId_Member3, N'Male', 180.0, 85.0, N'Giảm cân & Cải thiện sức bền', N'Heavy', N'Afternoon', N'Quyết tâm giảm 5kg trong 2 tháng');
    END;

    COMMIT TRANSACTION;
    PRINT N'[FlexFitIdentityDb] Seeded successfully.';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT N'[FlexFitIdentityDb] Error seeding: ' + ERROR_MESSAGE();
END CATCH;
GO

-- ----------------------------------------------------------------------------
-- 2. SEED FOR FlexFitCatalogDb
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'FlexFitCatalogDb')
BEGIN
    CREATE DATABASE FlexFitCatalogDb;
END;
GO

USE FlexFitCatalogDb;
GO

BEGIN TRANSACTION;
BEGIN TRY
    IF OBJECT_ID('Gyms', 'U') IS NOT NULL
    BEGIN
        DELETE FROM ClassSchedules;
        DELETE FROM Classes;
        DELETE FROM GymSessions;
        DELETE FROM BranchAmenityMappings;
        DELETE FROM GymAmenities;
        DELETE FROM Categories;
        DELETE FROM BranchImages;
        DELETE FROM GymImages;
        DELETE FROM BranchStaffs;
        DELETE FROM Branches;
        DELETE FROM Gyms;

        DECLARE @UserId_OwnerFitZone UNIQUEIDENTIFIER = 'A2000000-0000-0000-0000-000000000001';
        DECLARE @UserId_OwnerCali UNIQUEIDENTIFIER = 'A2000000-0000-0000-0000-000000000002';
        DECLARE @UserId_StaffQ1 UNIQUEIDENTIFIER = 'A3000000-0000-0000-0000-000000000001';
        DECLARE @UserId_StaffQ7 UNIQUEIDENTIFIER = 'A3000000-0000-0000-0000-000000000002';

        DECLARE @Cat_Yoga UNIQUEIDENTIFIER = 'C1000000-0000-0000-0000-000000000001';
        DECLARE @Cat_Pilates UNIQUEIDENTIFIER = 'C1000000-0000-0000-0000-000000000002';
        DECLARE @Cat_Boxing UNIQUEIDENTIFIER = 'C1000000-0000-0000-0000-000000000003';
        DECLARE @Cat_Kickboxing UNIQUEIDENTIFIER = 'C1000000-0000-0000-0000-000000000004';
        DECLARE @Cat_Dance UNIQUEIDENTIFIER = 'C1000000-0000-0000-0000-000000000005';
        DECLARE @Cat_Crossfit UNIQUEIDENTIFIER = 'C1000000-0000-0000-0000-000000000006';
        DECLARE @Cat_HIIT UNIQUEIDENTIFIER = 'C1000000-0000-0000-0000-000000000007';
        DECLARE @Cat_Zumba UNIQUEIDENTIFIER = 'C1000000-0000-0000-0000-000000000008';

        INSERT INTO Categories (CategoryId, CategoryName, Description)
        VALUES
        (@Cat_Yoga, N'Yoga', N'Các lớp Yoga tĩnh tâm, cân bằng và dẻo dai'),
        (@Cat_Pilates, N'Pilates', N'Tập luyện thắt chặt cơ lõi và phục hồi vóc dáng'),
        (@Cat_Boxing, N'Boxing', N'Luyện tập đối kháng và phản xạ cao'),
        (@Cat_Kickboxing, N'Kickboxing', N'Kết hợp đòn chân và đòn tay năng động'),
        (@Cat_Dance, N'Dance', N'Lớp nhảy giải phóng năng lượng'),
        (@Cat_Crossfit, N'Crossfit', N'Tập luyện thể lực tổng hợp cường độ cao'),
        (@Cat_HIIT, N'HIIT', N'Bài tập đốt mỡ siêu tốc'),
        (@Cat_Zumba, N'Zumba', N'Vũ điệu Zumba sôi động');

        DECLARE @Amenity_Parking UNIQUEIDENTIFIER = 'E1000000-0000-0000-0000-000000000001';
        DECLARE @Amenity_Shower UNIQUEIDENTIFIER = 'E1000000-0000-0000-0000-000000000002';
        DECLARE @Amenity_Locker UNIQUEIDENTIFIER = 'E1000000-0000-0000-0000-000000000003';
        DECLARE @Amenity_AC UNIQUEIDENTIFIER = 'E1000000-0000-0000-0000-000000000004';
        DECLARE @Amenity_Wifi UNIQUEIDENTIFIER = 'E1000000-0000-0000-0000-000000000005';
        DECLARE @Amenity_Towel UNIQUEIDENTIFIER = 'E1000000-0000-0000-0000-000000000006';

        INSERT INTO GymAmenities (AmenityId, AmenityName)
        VALUES
        (@Amenity_Parking, N'Parking'),
        (@Amenity_Shower, N'Shower'),
        (@Amenity_Locker, N'Locker'),
        (@Amenity_AC, N'AirConditioner'),
        (@Amenity_Wifi, N'Wifi'),
        (@Amenity_Towel, N'Towel');

        DECLARE @Gym_FitZone UNIQUEIDENTIFIER = 'B1000000-0000-0000-0000-000000000001';
        DECLARE @Gym_Cali UNIQUEIDENTIFIER = 'B1000000-0000-0000-0000-000000000002';

        INSERT INTO Gyms (GymId, OwnerId, GymName, Description, ThumbnailUrl, PhoneNumber, Email, Status, RatingAverage, TotalReviews, CreatedAt)
        VALUES
        (@Gym_FitZone, @UserId_OwnerFitZone, N'FitZone Fitness & Martial Arts', N'Hệ thống phòng tập hiện đại bậc nhất với trang thiết bị nhập khẩu Châu Âu.', N'https://images.unsplash.com/photo-1534438327276-14e5300c3a48', N'02873001111', N'info@fitzone.vn', 'Approved', 4.80, 12, GETUTCDATE()),
        (@Gym_Cali, @UserId_OwnerCali, N'California Fitness & Yoga Centre', N'Thương hiệu thể hình hàng đầu với không gian sang trọng và các HLV quốc tế.', N'https://images.unsplash.com/photo-1540497077202-7c8a3999166f', N'02873002222', N'contact@cali.vn', 'Approved', 4.90, 25, GETUTCDATE());

        DECLARE @Branch_FitZoneQ1 UNIQUEIDENTIFIER = 'B2000000-0000-0000-0000-000000000001';
        DECLARE @Branch_FitZoneQ7 UNIQUEIDENTIFIER = 'B2000000-0000-0000-0000-000000000002';
        DECLARE @Branch_CaliQ2 UNIQUEIDENTIFIER = 'B2000000-0000-0000-0000-000000000003';

        INSERT INTO Branches (BranchId, GymId, BranchName, Address, City, District, CreditCost, OpenTime, CloseTime, ThumbnailUrl, IsActive, CreatedAt)
        VALUES
        (@Branch_FitZoneQ1, @Gym_FitZone, N'FitZone Chi Nhánh Nguyễn Huệ', N'123 Nguyễn Huệ, Phường Bến Nghé', N'Hồ Chí Minh', N'Quận 1', 15, '06:00:00', '22:00:00', N'https://images.unsplash.com/photo-1571902943202-507ec2618e8f', 1, GETUTCDATE()),
        (@Branch_FitZoneQ7, @Gym_FitZone, N'FitZone Chi Nhánh Nguyễn Thị Thập', N'456 Nguyễn Thị Thập, Phường Tân Quy', N'Hồ Chí Minh', N'Quận 7', 12, '06:00:00', '21:30:00', N'https://images.unsplash.com/photo-1517838277536-f5f99be501cd', 1, GETUTCDATE()),
        (@Branch_CaliQ2, @Gym_Cali, N'California Chi Nhánh Thảo Điền', N'789 Xa Lộ Hà Nội, Phường Thảo Điền', N'Hồ Chí Minh', N'Quận 2', 20, '05:30:00', '23:00:00', N'https://images.unsplash.com/photo-1584735935682-2f2b69dff9d2', 1, GETUTCDATE());

        INSERT INTO BranchStaffs (StaffId, BranchId, AssignedAt)
        VALUES
        (@UserId_StaffQ1, @Branch_FitZoneQ1, GETUTCDATE()),
        (@UserId_StaffQ7, @Branch_FitZoneQ7, GETUTCDATE());

        INSERT INTO BranchAmenityMappings (BranchId, AmenityId)
        VALUES
        (@Branch_FitZoneQ1, @Amenity_Parking), (@Branch_FitZoneQ1, @Amenity_Shower), (@Branch_FitZoneQ1, @Amenity_Locker), (@Branch_FitZoneQ1, @Amenity_AC), (@Branch_FitZoneQ1, @Amenity_Wifi),
        (@Branch_FitZoneQ7, @Amenity_Parking), (@Branch_FitZoneQ7, @Amenity_Shower), (@Branch_FitZoneQ7, @Amenity_Locker),
        (@Branch_CaliQ2, @Amenity_Parking), (@Branch_CaliQ2, @Amenity_Shower), (@Branch_CaliQ2, @Amenity_Locker), (@Branch_CaliQ2, @Amenity_AC), (@Branch_CaliQ2, @Amenity_Wifi), (@Branch_CaliQ2, @Amenity_Towel);

        DECLARE @Session_Q1_Morning UNIQUEIDENTIFIER = 'F1000000-0000-0000-0000-000000000001';
        DECLARE @Session_Q1_Evening UNIQUEIDENTIFIER = 'F1000000-0000-0000-0000-000000000002';
        DECLARE @Session_Q7_Afternoon UNIQUEIDENTIFIER = 'F1000000-0000-0000-0000-000000000003';

        INSERT INTO GymSessions (SessionId, BranchId, SessionName, StartTime, EndTime, Capacity, CreditCost, Status, CreatedAt)
        VALUES
        (@Session_Q1_Morning, @Branch_FitZoneQ1, N'Tập Gym Tự Do - Ca Sáng', DATEADD(HOUR, 7, CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME2)), DATEADD(HOUR, 11, CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME2)), 30, 15, 'Open', GETUTCDATE()),
        (@Session_Q1_Evening, @Branch_FitZoneQ1, N'Tập Gym Tự Do - Ca Tối', DATEADD(HOUR, 17, CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME2)), DATEADD(HOUR, 21, CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME2)), 40, 15, 'Open', GETUTCDATE()),
        (@Session_Q7_Afternoon, @Branch_FitZoneQ7, N'Tập Gym Tự Do - Ca Chiều', DATEADD(HOUR, 13, CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME2)), DATEADD(HOUR, 17, CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME2)), 25, 12, 'Open', GETUTCDATE());

        DECLARE @Class_YogaQ1 UNIQUEIDENTIFIER = 'C2000000-0000-0000-0000-000000000001';
        DECLARE @Class_BoxingQ1 UNIQUEIDENTIFIER = 'C2000000-0000-0000-0000-000000000002';
        DECLARE @Class_HIITCali UNIQUEIDENTIFIER = 'C2000000-0000-0000-0000-000000000003';

        INSERT INTO Classes (ClassId, BranchId, CategoryId, ClassName, Description, CoachName, StartTime, EndTime, Capacity, CreditCost, DifficultyLevel, CaloriesBurnEstimate, ThumbnailUrl, Status, CreatedAt)
        VALUES
        (@Class_YogaQ1, @Branch_FitZoneQ1, @Cat_Yoga, N'Vinyasa Flow Yoga', N'Lớp Yoga giải tỏa căng thẳng và tăng cường độ dẻo dai toàn thân.', N'HLV Master Kamal', DATEADD(HOUR, 8, CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME2)), DATEADD(HOUR, 9, CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME2)), 15, 18, N'Intermediate', 350, N'https://images.unsplash.com/photo-1545205597-3d9d02c29597', 'Open', GETUTCDATE()),
        (@Class_BoxingQ1, @Branch_FitZoneQ1, @Cat_Boxing, N'Boxing Power & Core', N'Rèn luyện kỹ thuật đấm boxing cơ bản và tăng sức bền cơ bắp.', N'HLV Nguyễn Văn Hùng', DATEADD(HOUR, 18, CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME2)), DATEADD(HOUR, 19, CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME2)), 12, 20, N'Advanced', 550, N'https://images.unsplash.com/photo-1549719386-74dfcbf7dbed', 'Open', GETUTCDATE()),
        (@Class_HIITCali, @Branch_CaliQ2, @Cat_HIIT, N'HIIT Fat Burner Express', N'Đốt cháy tới 600 calo trong 45 phút tập luyện cường độ cao ngắt quãng.', N'HLV Sarah Tran', DATEADD(HOUR, 19, CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME2)), DATEADD(HOUR, 20, CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME2)), 20, 25, N'Hard', 600, N'https://images.unsplash.com/photo-1518611012118-696072aa579a', 'Open', GETUTCDATE());
    END;

    COMMIT TRANSACTION;
    PRINT N'[FlexFitCatalogDb] Seeded successfully.';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT N'[FlexFitCatalogDb] Error seeding: ' + ERROR_MESSAGE();
END CATCH;
GO

-- ----------------------------------------------------------------------------
-- 3. SEED FOR FlexFitBookingDb
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'FlexFitBookingDb')
BEGIN
    CREATE DATABASE FlexFitBookingDb;
END;
GO

USE FlexFitBookingDb;
GO

BEGIN TRANSACTION;
BEGIN TRY
    IF OBJECT_ID('GymBookings', 'U') IS NOT NULL
    BEGIN
        DELETE FROM CheckInLogs;
        DELETE FROM ClassBookings;
        DELETE FROM GymBookings;

        DECLARE @UserId_StaffQ1 UNIQUEIDENTIFIER = 'A3000000-0000-0000-0000-000000000001';
        DECLARE @UserId_Member1 UNIQUEIDENTIFIER = 'A4000000-0000-0000-0000-000000000001';
        DECLARE @UserId_Member2 UNIQUEIDENTIFIER = 'A4000000-0000-0000-0000-000000000002';

        DECLARE @Session_Q1_Morning UNIQUEIDENTIFIER = 'F1000000-0000-0000-0000-000000000001';
        DECLARE @Class_YogaQ1 UNIQUEIDENTIFIER = 'C2000000-0000-0000-0000-000000000001';

        DECLARE @Booking_Gym1 UNIQUEIDENTIFIER = 'D1000000-0000-0000-0000-000000000001';
        DECLARE @Booking_Class1 UNIQUEIDENTIFIER = 'D2000000-0000-0000-0000-000000000001';

        DECLARE @Branch_FitZoneQ1 UNIQUEIDENTIFIER = 'B2000000-0000-0000-0000-000000000001';
        DECLARE @Gym_FitZone UNIQUEIDENTIFIER = 'B1000000-0000-0000-0000-000000000001';

        INSERT INTO GymBookings (BookingId, UserId, SessionId, BranchId, GymId, BookingCode, CreditUsed, GymNameSnapshot, SessionNameSnapshot, BranchNameSnapshot, BranchAddressSnapshot, StartTimeSnapshot, EndTimeSnapshot, CheckInStatus, Status, BookedAt, CheckedInBy, CheckInTime, RefundCredit, IsReminded3h, IsReminded1h)
        VALUES
        (@Booking_Gym1, @UserId_Member1, @Session_Q1_Morning, @Branch_FitZoneQ1, @Gym_FitZone, N'GBK-998231', 15, N'FitZone Fitness & Martial Arts', N'Tập Gym Tự Do - Ca Sáng', N'FitZone Chi Nhánh Nguyễn Huệ', N'123 Nguyễn Huệ, Phường Bến Nghé', DATEADD(HOUR, 7, CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME2)), DATEADD(HOUR, 11, CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME2)), 'CheckedIn', 'Completed', DATEADD(DAY, -1, GETUTCDATE()), @UserId_StaffQ1, DATEADD(HOUR, -24, GETUTCDATE()), 0, 0, 0);

        INSERT INTO ClassBookings (BookingId, UserId, ClassId, BranchId, GymId, BookingCode, CreditUsed, GymNameSnapshot, ClassNameSnapshot, BranchNameSnapshot, BranchAddressSnapshot, CoachNameSnapshot, StartTimeSnapshot, EndTimeSnapshot, CheckInStatus, Status, BookedAt, RefundCredit, IsReminded3h, IsReminded1h)
        VALUES
        (@Booking_Class1, @UserId_Member2, @Class_YogaQ1, @Branch_FitZoneQ1, @Gym_FitZone, N'CBK-114820', 18, N'FitZone Fitness & Martial Arts', N'Vinyasa Flow Yoga', N'FitZone Chi Nhánh Nguyễn Huệ', N'123 Nguyễn Huệ, Phường Bến Nghé', N'HLV Master Kamal', DATEADD(HOUR, 8, CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME2)), DATEADD(HOUR, 9, CAST(CAST(GETUTCDATE() AS DATE) AS DATETIME2)), 'NotCheckedIn', 'Booked', GETUTCDATE(), 0, 0, 0);

        INSERT INTO CheckInLogs (CheckInLogId, UserId, GymBookingId, ClassBookingId, ScannedBy, Status, Message, ScannedAt)
        VALUES
        (NEWID(), @UserId_Member1, @Booking_Gym1, NULL, @UserId_StaffQ1, N'Success', N'Check-in hợp lệ tại FitZone Q1', DATEADD(HOUR, -24, GETUTCDATE()));
    END;

    COMMIT TRANSACTION;
    PRINT N'[FlexFitBookingDb] Seeded successfully.';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT N'[FlexFitBookingDb] Error seeding: ' + ERROR_MESSAGE();
END CATCH;
GO

-- ----------------------------------------------------------------------------
-- 4. SEED FOR FlexFitPaymentDb
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'FlexFitPaymentDb')
BEGIN
    CREATE DATABASE FlexFitPaymentDb;
END;
GO

USE FlexFitPaymentDb;
GO

BEGIN TRANSACTION;
BEGIN TRY
    IF OBJECT_ID('CreditPackages', 'U') IS NOT NULL
    BEGIN
        DELETE FROM Payments;
        DELETE FROM CreditTransactions;
        DELETE FROM UserCredits;
        DELETE FROM CreditPackages;

        DECLARE @UserId_Member1 UNIQUEIDENTIFIER = 'A4000000-0000-0000-0000-000000000001';
        DECLARE @UserId_Member2 UNIQUEIDENTIFIER = 'A4000000-0000-0000-0000-000000000002';
        DECLARE @UserId_Member3 UNIQUEIDENTIFIER = 'A4000000-0000-0000-0000-000000000003';

        DECLARE @Pkg_Starter UNIQUEIDENTIFIER = 'E1000000-0000-0000-0000-000000000001';
        DECLARE @Pkg_Standard UNIQUEIDENTIFIER = 'E1000000-0000-0000-0000-000000000002';
        DECLARE @Pkg_VIP UNIQUEIDENTIFIER = 'E1000000-0000-0000-0000-000000000003';

        INSERT INTO CreditPackages (PackageId, PackageName, CreditAmount, BonusCredit, Price, Description, IsPopular, IsActive, CreatedAt)
        VALUES
        (@Pkg_Starter, N'Gói Starter', 50, 0, 199000, N'Phù hợp người mới bắt đầu trải nghiệm tập luyện', 0, 1, GETUTCDATE()),
        (@Pkg_Standard, N'Gói Standard', 120, 10, 399000, N'Gói phổ biến được yêu thích nhất cho người tập thường xuyên', 1, 1, GETUTCDATE()),
        (@Pkg_VIP, N'Gói VIP Premium', 300, 50, 899000, N'Gói tiết kiệm nhất dành cho các tín đồ thể thao chuyên nghiệp', 0, 1, GETUTCDATE());

        INSERT INTO UserCredits (UserCreditId, UserId, Balance, TotalEarned, TotalSpent, UpdatedAt)
        VALUES
        (NEWID(), @UserId_Member1, 150, 180, 30, GETUTCDATE()),
        (NEWID(), @UserId_Member2, 50, 50, 0, GETUTCDATE()),
        (NEWID(), @UserId_Member3, 0, 0, 0, GETUTCDATE());

        INSERT INTO CreditTransactions (TransactionId, UserId, Amount, BalanceBefore, BalanceAfter, Type, Description, CreatedAt)
        VALUES
        (NEWID(), @UserId_Member1, 180, 0, 180, N'TopUp', N'Nạp gói Credit Standard (+10 Bonus)', DATEADD(DAY, -5, GETUTCDATE())),
        (NEWID(), @UserId_Member1, -15, 180, 165, N'GymBooking', N'Đặt lịch tập Gym tại FitZone Q1', DATEADD(DAY, -2, GETUTCDATE())),
        (NEWID(), @UserId_Member1, -15, 165, 150, N'GymBooking', N'Đặt lịch tập Gym ca tối tại FitZone Q1', GETUTCDATE()),
        (NEWID(), @UserId_Member2, 50, 0, 50, N'TopUp', N'Nạp gói Credit Starter', DATEADD(DAY, -1, GETUTCDATE()));

        INSERT INTO Payments (PaymentId, UserId, PackageId, Amount, PaymentMethod, ProviderTransactionCode, Status, PaidAt, CreatedAt)
        VALUES
        (NEWID(), @UserId_Member1, @Pkg_Standard, 399000, N'VNPay', N'VNP14859302', 'Completed', DATEADD(DAY, -5, GETUTCDATE()), DATEADD(DAY, -5, GETUTCDATE())),
        (NEWID(), @UserId_Member2, @Pkg_Starter, 199000, N'MoMo', N'MM88492019', 'Completed', DATEADD(DAY, -1, GETUTCDATE()), DATEADD(DAY, -1, GETUTCDATE()));
    END;

    COMMIT TRANSACTION;
    PRINT N'[FlexFitPaymentDb] Seeded successfully.';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT N'[FlexFitPaymentDb] Error seeding: ' + ERROR_MESSAGE();
END CATCH;
GO

-- ----------------------------------------------------------------------------
-- 5. SEED FOR FlexFitDB (Engagement & Recommendations)
-- ----------------------------------------------------------------------------
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = 'FlexFitDB')
BEGIN
    CREATE DATABASE FlexFitDB;
END;
GO

USE FlexFitDB;
GO

BEGIN TRANSACTION;
BEGIN TRY
    IF OBJECT_ID('Notifications', 'U') IS NOT NULL
    BEGIN
        DELETE FROM UserWorkoutHistories;
        DELETE FROM Notifications;
        DELETE FROM FavoriteClasses;
        DELETE FROM FavoriteGyms;
        DELETE FROM Reviews;

        DECLARE @UserId_Member1 UNIQUEIDENTIFIER = 'A4000000-0000-0000-0000-000000000001';
        DECLARE @UserId_Member2 UNIQUEIDENTIFIER = 'A4000000-0000-0000-0000-000000000002';

        DECLARE @Gym_FitZone UNIQUEIDENTIFIER = 'B1000000-0000-0000-0000-000000000001';
        DECLARE @Gym_Cali UNIQUEIDENTIFIER = 'B1000000-0000-0000-0000-000000000002';

        DECLARE @Class_YogaQ1 UNIQUEIDENTIFIER = 'C2000000-0000-0000-0000-000000000001';
        DECLARE @Class_BoxingQ1 UNIQUEIDENTIFIER = 'C2000000-0000-0000-0000-000000000002';

        DECLARE @Booking_Gym1 UNIQUEIDENTIFIER = 'D1000000-0000-0000-0000-000000000001';

        INSERT INTO Reviews (ReviewId, UserId, BookingId, GymId, ClassId, Rating, Comment, CreatedAt, GymBookingId)
        VALUES
        (NEWID(), @UserId_Member1, @Booking_Gym1, @Gym_FitZone, NULL, 5, N'Phòng tập cực kỳ sạch sẽ, máy móc hiện đại và nhân viên hỗ trợ nhiệt tình!', DATEADD(HOUR, -20, GETUTCDATE()), @Booking_Gym1);

        INSERT INTO FavoriteGyms (UserId, GymId, CreatedAt)
        VALUES
        (@UserId_Member1, @Gym_FitZone, GETUTCDATE()),
        (@UserId_Member2, @Gym_Cali, GETUTCDATE());

        INSERT INTO FavoriteClasses (UserId, ClassId, CreatedAt)
        VALUES
        (@UserId_Member1, @Class_BoxingQ1, GETUTCDATE()),
        (@UserId_Member2, @Class_YogaQ1, GETUTCDATE());

        INSERT INTO Notifications (NotificationId, UserId, Title, Content, Type, IsRead, CreatedAt)
        VALUES
        (NEWID(), @UserId_Member1, N'Nạp Credit Thành Công', N'Bạn đã nạp thành công 180 Credits vào tài khoản FlexFit.', N'Payment', 1, DATEADD(DAY, -5, GETUTCDATE())),
        (NEWID(), @UserId_Member1, N'Nhắc Nhở Tập Luyện', N'Ca tập Gym của bạn tại FitZone Q1 sẽ bắt đầu sau 1 giờ.', N'BookingReminder', 0, GETUTCDATE());

        INSERT INTO UserWorkoutHistories (WorkoutHistoryId, UserId, GymBookingId, ClassBookingId, CaloriesBurned, WorkoutDurationMinutes, CreatedAt)
        VALUES
        (NEWID(), @UserId_Member1, @Booking_Gym1, NULL, 450, 75, DATEADD(HOUR, -23, GETUTCDATE()));
    END;

    COMMIT TRANSACTION;
    PRINT N'[FlexFitDB] Seeded successfully.';
END TRY
BEGIN CATCH
    ROLLBACK TRANSACTION;
    PRINT N'[FlexFitDB] Error seeding: ' + ERROR_MESSAGE();
END CATCH;
GO

PRINT N'=== ALL MICROSERVICES DATABASES SEEDED SUCCESSFULLY! ===';
GO
