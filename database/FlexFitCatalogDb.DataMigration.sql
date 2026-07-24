-- ===================================================
-- DATA MIGRATION FOR FLEXFIT CATALOG SERVICE
-- Source Database: FlexFitDb (Please edit if different)
-- Target Database: FlexFitCatalogDb
-- ===================================================

USE [FlexFitCatalogDb];
GO

-- 1. Migrate Categories
PRINT 'Migrating Categories...';
INSERT INTO [Categories] ([CategoryId], [CategoryName], [Description])
SELECT s.[CategoryId], s.[CategoryName], s.[Description]
FROM [FlexFitDb].[dbo].[Categories] s
WHERE NOT EXISTS (
    SELECT 1 FROM [Categories] t WHERE t.[CategoryId] = s.[CategoryId]
);

-- 2. Migrate GymAmenities
PRINT 'Migrating GymAmenities...';
INSERT INTO [GymAmenities] ([AmenityId], [AmenityName])
SELECT s.[AmenityId], s.[AmenityName]
FROM [FlexFitDb].[dbo].[GymAmenities] s
WHERE NOT EXISTS (
    SELECT 1 FROM [GymAmenities] t WHERE t.[AmenityId] = s.[AmenityId]
);

-- 3. Migrate Gyms
PRINT 'Migrating Gyms...';
INSERT INTO [Gyms] ([GymId], [OwnerId], [GymName], [Description], [ThumbnailUrl], [PhoneNumber], [Email], [Status], [RatingAverage], [TotalReviews], [CreatedAt], [UpdatedAt])
SELECT s.[GymId], s.[OwnerId], s.[GymName], s.[Description], s.[ThumbnailUrl], s.[PhoneNumber], s.[Email], s.[Status], s.[RatingAverage], s.[TotalReviews], s.[CreatedAt], s.[UpdatedAt]
FROM [FlexFitDb].[dbo].[Gyms] s
WHERE NOT EXISTS (
    SELECT 1 FROM [Gyms] t WHERE t.[GymId] = s.[GymId]
);

-- 4. Migrate GymImages
PRINT 'Migrating GymImages...';
INSERT INTO [GymImages] ([GymImageId], [GymId], [ImageUrl], [DisplayOrder])
SELECT s.[GymImageId], s.[GymId], s.[ImageUrl], s.[DisplayOrder]
FROM [FlexFitDb].[dbo].[GymImages] s
WHERE NOT EXISTS (
    SELECT 1 FROM [GymImages] t WHERE t.[GymImageId] = s.[GymImageId]
);

-- 5. Migrate Branches
PRINT 'Migrating Branches...';
INSERT INTO [Branches] ([BranchId], [GymId], [BranchName], [Address], [City], [District], [CreditCost], [OpenTime], [CloseTime], [ThumbnailUrl], [IsActive], [CreatedAt], [UpdatedAt])
SELECT s.[BranchId], s.[GymId], s.[BranchName], s.[Address], s.[City], s.[District], s.[CreditCost], s.[OpenTime], s.[CloseTime], s.[ThumbnailUrl], s.[IsActive], s.[CreatedAt], s.[UpdatedAt]
FROM [FlexFitDb].[dbo].[Branches] s
WHERE NOT EXISTS (
    SELECT 1 FROM [Branches] t WHERE t.[BranchId] = s.[BranchId]
);

-- 6. Migrate BranchImages
PRINT 'Migrating BranchImages...';
INSERT INTO [BranchImages] ([BranchImageId], [BranchId], [ImageUrl], [DisplayOrder])
SELECT s.[BranchImageId], s.[BranchId], s.[ImageUrl], s.[DisplayOrder]
FROM [FlexFitDb].[dbo].[BranchImages] s
WHERE NOT EXISTS (
    SELECT 1 FROM [BranchImages] t WHERE t.[BranchImageId] = s.[BranchImageId]
);

-- 7. Migrate BranchStaffs
PRINT 'Migrating BranchStaffs...';
INSERT INTO [BranchStaffs] ([StaffId], [BranchId], [AssignedAt])
SELECT s.[StaffId], s.[BranchId], s.[AssignedAt]
FROM [FlexFitDb].[dbo].[BranchStaffs] s
WHERE NOT EXISTS (
    SELECT 1 FROM [BranchStaffs] t WHERE t.[StaffId] = s.[StaffId] AND t.[BranchId] = s.[BranchId]
);

-- 8. Migrate BranchAmenityMappings
PRINT 'Migrating BranchAmenityMappings...';
INSERT INTO [BranchAmenityMappings] ([BranchId], [AmenityId])
SELECT s.[BranchId], s.[AmenityId]
FROM [FlexFitDb].[dbo].[BranchAmenityMappings] s
WHERE NOT EXISTS (
    SELECT 1 FROM [BranchAmenityMappings] t WHERE t.[BranchId] = s.[BranchId] AND t.[AmenityId] = s.[AmenityId]
);

-- 9. Migrate Classes
PRINT 'Migrating Classes...';
INSERT INTO [Classes] ([ClassId], [BranchId], [CategoryId], [ClassName], [Description], [CoachName], [StartTime], [EndTime], [Capacity], [CreditCost], [DifficultyLevel], [CaloriesBurnEstimate], [ThumbnailUrl], [Status], [CreatedAt], [UpdatedAt])
SELECT s.[ClassId], s.[BranchId], s.[CategoryId], s.[ClassName], s.[Description], s.[CoachName], s.[StartTime], s.[EndTime], s.[Capacity], s.[CreditCost], s.[DifficultyLevel], s.[CaloriesBurnEstimate], s.[ThumbnailUrl], s.[Status], s.[CreatedAt], s.[UpdatedAt]
FROM [FlexFitDb].[dbo].[Classes] s
WHERE NOT EXISTS (
    SELECT 1 FROM [Classes] t WHERE t.[ClassId] = s.[ClassId]
);

-- 10. Migrate ClassSchedules
PRINT 'Migrating ClassSchedules...';
INSERT INTO [ClassSchedules] ([ScheduleId], [ClassId], [DayOfWeek], [StartHour], [EndHour])
SELECT s.[ScheduleId], s.[ClassId], s.[DayOfWeek], s.[StartHour], s.[EndHour]
FROM [FlexFitDb].[dbo].[ClassSchedules] s
WHERE NOT EXISTS (
    SELECT 1 FROM [ClassSchedules] t WHERE t.[ScheduleId] = s.[ScheduleId]
);

-- 11. Migrate GymSessions
PRINT 'Migrating GymSessions...';
INSERT INTO [GymSessions] ([SessionId], [BranchId], [SessionName], [StartTime], [EndTime], [Capacity], [CreditCost], [Status], [CreatedAt], [UpdatedAt])
SELECT s.[SessionId], s.[BranchId], s.[SessionName], s.[StartTime], s.[EndTime], s.[Capacity], s.[CreditCost], s.[Status], s.[CreatedAt], s.[UpdatedAt]
FROM [FlexFitDb].[dbo].[GymSessions] s
WHERE NOT EXISTS (
    SELECT 1 FROM [GymSessions] t WHERE t.[SessionId] = s.[SessionId]
);

-- 12. Migrate FavoriteGyms
PRINT 'Migrating FavoriteGyms...';
INSERT INTO [FavoriteGyms] ([UserId], [GymId], [CreatedAt])
SELECT s.[UserId], s.[GymId], s.[CreatedAt]
FROM [FlexFitDb].[dbo].[FavoriteGyms] s
WHERE NOT EXISTS (
    SELECT 1 FROM [FavoriteGyms] t WHERE t.[UserId] = s.[UserId] AND t.[GymId] = s.[GymId]
);

-- 13. Migrate FavoriteClasses
PRINT 'Migrating FavoriteClasses...';
INSERT INTO [FavoriteClasses] ([UserId], [ClassId], [CreatedAt])
SELECT s.[UserId], s.[ClassId], s.[CreatedAt]
FROM [FlexFitDb].[dbo].[FavoriteClasses] s
WHERE NOT EXISTS (
    SELECT 1 FROM [FavoriteClasses] t WHERE t.[UserId] = s.[UserId] AND t.[ClassId] = s.[ClassId]
);

PRINT 'Data migration completed successfully!';
