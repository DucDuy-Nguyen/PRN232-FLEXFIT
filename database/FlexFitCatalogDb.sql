-- ===================================================
-- SCHEMA OF FLEXFIT CATALOG SERVICE
-- Database Name: FlexFitCatalogDb
-- Target Engine: Microsoft SQL Server
-- Description: Independent schema for FlexFit.CatalogService microservice
-- ===================================================
USE master;
GO
IF NOT EXISTS (SELECT * FROM sys.databases WHERE name = N'FlexFitCatalogDb')
BEGIN
    CREATE DATABASE [FlexFitCatalogDb];
END;
GO
USE [FlexFitCatalogDb];
GO

IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716130930_InitialCatalogDb'
)
BEGIN
    CREATE TABLE [Categories] (
        [CategoryId] uniqueidentifier NOT NULL DEFAULT ((newid())),
        [CategoryName] nvarchar(100) NOT NULL,
        [Description] nvarchar(255) NULL,
        CONSTRAINT [PK__Categori__19093A0BC8C5ABA1] PRIMARY KEY ([CategoryId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716130930_InitialCatalogDb'
)
BEGIN
    CREATE TABLE [GymAmenities] (
        [AmenityId] uniqueidentifier NOT NULL DEFAULT ((newid())),
        [AmenityName] nvarchar(100) NOT NULL,
        CONSTRAINT [PK__GymAmeni__842AF50BF3A9D836] PRIMARY KEY ([AmenityId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716130930_InitialCatalogDb'
)
BEGIN
    CREATE TABLE [Gyms] (
        [GymId] uniqueidentifier NOT NULL DEFAULT ((newid())),
        [OwnerId] uniqueidentifier NOT NULL,
        [GymName] nvarchar(150) NOT NULL,
        [Description] nvarchar(max) NULL,
        [ThumbnailUrl] nvarchar(max) NULL,
        [PhoneNumber] nvarchar(20) NULL,
        [Email] nvarchar(100) NULL,
        [Status] nvarchar(30) NOT NULL DEFAULT N'Pending',
        [RatingAverage] decimal(3,2) NOT NULL,
        [TotalReviews] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT ((getdate())),
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK__Gyms__1A3A7C967D8AF47B] PRIMARY KEY ([GymId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716130930_InitialCatalogDb'
)
BEGIN
    CREATE TABLE [Branches] (
        [BranchId] uniqueidentifier NOT NULL DEFAULT ((newid())),
        [GymId] uniqueidentifier NOT NULL,
        [BranchName] nvarchar(150) NOT NULL,
        [Address] nvarchar(255) NULL,
        [City] nvarchar(100) NULL,
        [District] nvarchar(100) NULL,
        [CreditCost] int NOT NULL,
        [OpenTime] time NULL,
        [CloseTime] time NULL,
        [ThumbnailUrl] nvarchar(max) NULL,
        [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
        [CreatedAt] datetime2 NOT NULL DEFAULT ((getdate())),
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK__Branches__A1682FC57F628C8E] PRIMARY KEY ([BranchId]),
        CONSTRAINT [FK__Branches__GymId__5812160E] FOREIGN KEY ([GymId]) REFERENCES [Gyms] ([GymId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716130930_InitialCatalogDb'
)
BEGIN
    CREATE TABLE [FavoriteGyms] (
        [UserId] uniqueidentifier NOT NULL,
        [GymId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT ((getdate())),
        CONSTRAINT [PK_FavoriteGyms] PRIMARY KEY ([UserId], [GymId]),
        CONSTRAINT [FK__FavoriteG__GymId__4B7734FF] FOREIGN KEY ([GymId]) REFERENCES [Gyms] ([GymId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716130930_InitialCatalogDb'
)
BEGIN
    CREATE TABLE [GymImages] (
        [GymImageId] uniqueidentifier NOT NULL DEFAULT ((newid())),
        [GymId] uniqueidentifier NOT NULL,
        [ImageUrl] nvarchar(max) NOT NULL,
        [DisplayOrder] int NOT NULL,
        CONSTRAINT [PK__GymImage__659DCAAE6F641A53] PRIMARY KEY ([GymImageId]),
        CONSTRAINT [FK__GymImages__GymId__619B8048] FOREIGN KEY ([GymId]) REFERENCES [Gyms] ([GymId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716130930_InitialCatalogDb'
)
BEGIN
    CREATE TABLE [BranchAmenityMappings] (
        [BranchId] uniqueidentifier NOT NULL,
        [AmenityId] uniqueidentifier NOT NULL,
        CONSTRAINT [PK_BranchAmenityMappings] PRIMARY KEY ([BranchId], [AmenityId]),
        CONSTRAINT [FK__BranchAme__Ameni__6E01572D] FOREIGN KEY ([AmenityId]) REFERENCES [GymAmenities] ([AmenityId]),
        CONSTRAINT [FK__BranchAme__Branc__6D0D32F4] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([BranchId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716130930_InitialCatalogDb'
)
BEGIN
    CREATE TABLE [BranchImages] (
        [BranchImageId] uniqueidentifier NOT NULL DEFAULT ((newid())),
        [BranchId] uniqueidentifier NOT NULL,
        [ImageUrl] nvarchar(max) NOT NULL,
        [DisplayOrder] int NOT NULL,
        CONSTRAINT [PK__BranchIm__DEDBCB2E0DD9038B] PRIMARY KEY ([BranchImageId]),
        CONSTRAINT [FK__BranchIma__Branc__66603565] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([BranchId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716130930_InitialCatalogDb'
)
BEGIN
    CREATE TABLE [BranchStaffs] (
        [StaffId] uniqueidentifier NOT NULL,
        [BranchId] uniqueidentifier NOT NULL,
        [AssignedAt] datetime2 NOT NULL DEFAULT ((getdate())),
        CONSTRAINT [PK_BranchStaffs] PRIMARY KEY ([StaffId], [BranchId]),
        CONSTRAINT [FK_BranchStaffs_Branches] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([BranchId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716130930_InitialCatalogDb'
)
BEGIN
    CREATE TABLE [Classes] (
        [ClassId] uniqueidentifier NOT NULL DEFAULT ((newid())),
        [BranchId] uniqueidentifier NOT NULL,
        [CategoryId] uniqueidentifier NOT NULL,
        [ClassName] nvarchar(150) NOT NULL,
        [Description] nvarchar(max) NULL,
        [CoachName] nvarchar(100) NULL,
        [StartTime] datetime2 NOT NULL,
        [EndTime] datetime2 NOT NULL,
        [Capacity] int NOT NULL,
        [CreditCost] int NOT NULL,
        [DifficultyLevel] nvarchar(30) NULL,
        [CaloriesBurnEstimate] int NULL,
        [ThumbnailUrl] nvarchar(max) NULL,
        [Status] nvarchar(30) NOT NULL DEFAULT N'Open',
        [CreatedAt] datetime2 NOT NULL DEFAULT ((getdate())),
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK__Classes__CB1927C00781F387] PRIMARY KEY ([ClassId]),
        CONSTRAINT [FK__Classes__BranchI__7D439ABD] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([BranchId]),
        CONSTRAINT [FK__Classes__Categor__7E37BEF6] FOREIGN KEY ([CategoryId]) REFERENCES [Categories] ([CategoryId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716130930_InitialCatalogDb'
)
BEGIN
    CREATE TABLE [GymSessions] (
        [SessionId] uniqueidentifier NOT NULL DEFAULT ((newid())),
        [BranchId] uniqueidentifier NOT NULL,
        [SessionName] nvarchar(150) NULL,
        [StartTime] datetime2 NOT NULL,
        [EndTime] datetime2 NOT NULL,
        [Capacity] int NOT NULL,
        [CreditCost] int NOT NULL,
        [Status] nvarchar(30) NOT NULL DEFAULT N'Open',
        [CreatedAt] datetime2 NOT NULL DEFAULT ((getdate())),
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK__GymSessi__C9F492905AF95180] PRIMARY KEY ([SessionId]),
        CONSTRAINT [FK__GymSessio__Branc__778AC167] FOREIGN KEY ([BranchId]) REFERENCES [Branches] ([BranchId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716130930_InitialCatalogDb'
)
BEGIN
    CREATE TABLE [ClassSchedules] (
        [ScheduleId] uniqueidentifier NOT NULL DEFAULT ((newid())),
        [ClassId] uniqueidentifier NOT NULL,
        [DayOfWeek] int NOT NULL,
        [StartHour] time NOT NULL,
        [EndHour] time NOT NULL,
        CONSTRAINT [PK__ClassSch__9C8A5B49CB2537F1] PRIMARY KEY ([ScheduleId]),
        CONSTRAINT [FK__ClassSche__Class__02084FDA] FOREIGN KEY ([ClassId]) REFERENCES [Classes] ([ClassId])
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716130930_InitialCatalogDb'
)
BEGIN
    CREATE TABLE [FavoriteClasses] (
        [UserId] uniqueidentifier NOT NULL,
        [ClassId] uniqueidentifier NOT NULL,
        [CreatedAt] datetime2 NOT NULL DEFAULT ((getdate())),
        CONSTRAINT [PK_FavoriteClasses] PRIMARY KEY ([UserId], [ClassId]),
        CONSTRAINT [FK_FavoriteClasses_Classes_ClassId] FOREIGN KEY ([ClassId]) REFERENCES [Classes] ([ClassId]) ON DELETE CASCADE
    );
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716130930_InitialCatalogDb'
)
BEGIN
    CREATE INDEX [IX_BranchAmenityMappings_AmenityId] ON [BranchAmenityMappings] ([AmenityId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716130930_InitialCatalogDb'
)
BEGIN
    CREATE INDEX [IX_Branches_GymId] ON [Branches] ([GymId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716130930_InitialCatalogDb'
)
BEGIN
    CREATE INDEX [IX_BranchImages_BranchId] ON [BranchImages] ([BranchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716130930_InitialCatalogDb'
)
BEGIN
    CREATE INDEX [IX_BranchStaffs_BranchId] ON [BranchStaffs] ([BranchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716130930_InitialCatalogDb'
)
BEGIN
    CREATE UNIQUE INDEX [UQ__Categori__8517B2E053ED6742] ON [Categories] ([CategoryName]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716130930_InitialCatalogDb'
)
BEGIN
    CREATE INDEX [IX_Classes_BranchId] ON [Classes] ([BranchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716130930_InitialCatalogDb'
)
BEGIN
    CREATE INDEX [IX_Classes_CategoryId] ON [Classes] ([CategoryId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716130930_InitialCatalogDb'
)
BEGIN
    CREATE INDEX [IX_ClassSchedules_ClassId] ON [ClassSchedules] ([ClassId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716130930_InitialCatalogDb'
)
BEGIN
    CREATE INDEX [IX_FavoriteClasses_ClassId] ON [FavoriteClasses] ([ClassId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716130930_InitialCatalogDb'
)
BEGIN
    CREATE INDEX [IX_FavoriteGyms_GymId] ON [FavoriteGyms] ([GymId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716130930_InitialCatalogDb'
)
BEGIN
    CREATE UNIQUE INDEX [UQ__GymAmeni__7B4A459F1F1C7085] ON [GymAmenities] ([AmenityName]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716130930_InitialCatalogDb'
)
BEGIN
    CREATE INDEX [IX_GymImages_GymId] ON [GymImages] ([GymId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716130930_InitialCatalogDb'
)
BEGIN
    CREATE INDEX [IX_GymSessions_BranchId] ON [GymSessions] ([BranchId]);
END;
GO

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260716130930_InitialCatalogDb'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260716130930_InitialCatalogDb', N'8.0.0');
END;
GO

COMMIT;
GO

