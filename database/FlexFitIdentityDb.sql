CREATE DATABASE [FlexFitIdentityDb];
GO
USE [FlexFitIdentityDb];
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

CREATE TABLE [Roles] (
    [RoleId] uniqueidentifier NOT NULL DEFAULT ((newid())),
    [RoleName] nvarchar(50) NOT NULL,
    [Description] nvarchar(255) NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT ((getdate())),
    CONSTRAINT [PK__Roles__8AFACE1AD46A3A12] PRIMARY KEY ([RoleId])
);
GO

CREATE TABLE [Users] (
    [UserId] uniqueidentifier NOT NULL DEFAULT ((newid())),
    [FullName] nvarchar(100) NOT NULL,
    [Email] nvarchar(100) NOT NULL,
    [PasswordHash] nvarchar(max) NOT NULL,
    [PhoneNumber] nvarchar(20) NULL,
    [DateOfBirth] date NULL,
    [AvatarUrl] nvarchar(max) NULL,
    [IsEmailVerified] bit NOT NULL,
    [EmailVerificationToken] nvarchar(max) NULL,
    [VerificationTokenExpires] datetime2 NULL,
    [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit),
    [LastLoginAt] datetime2 NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT ((getdate())),
    [UpdatedAt] datetime2 NULL,
    CONSTRAINT [PK__Users__1788CC4C75B61DB2] PRIMARY KEY ([UserId])
);
GO

CREATE TABLE [MemberProfiles] (
    [MemberProfileId] uniqueidentifier NOT NULL DEFAULT ((newid())),
    [UserId] uniqueidentifier NOT NULL,
    [Gender] nvarchar(20) NULL,
    [HeightCm] decimal(5,2) NULL,
    [WeightKg] decimal(5,2) NULL,
    [FitnessGoal] nvarchar(255) NULL,
    [ActivityLevel] nvarchar(50) NULL,
    [PreferredWorkoutTime] nvarchar(50) NULL,
    [Bio] nvarchar(max) NULL,
    CONSTRAINT [PK__MemberPr__0485209F89155E84] PRIMARY KEY ([MemberProfileId]),
    CONSTRAINT [FK__MemberPro__UserI__49C3F6B7] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);
GO

CREATE TABLE [UserRoles] (
    [UserId] uniqueidentifier NOT NULL,
    [RoleId] uniqueidentifier NOT NULL,
    [AssignedAt] datetime2 NOT NULL DEFAULT ((getdate())),
    CONSTRAINT [PK_UserRoles] PRIMARY KEY ([UserId], [RoleId]),
    CONSTRAINT [FK_UserRoles_Roles] FOREIGN KEY ([RoleId]) REFERENCES [Roles] ([RoleId]) ON DELETE NO ACTION,
    CONSTRAINT [FK_UserRoles_Users] FOREIGN KEY ([UserId]) REFERENCES [Users] ([UserId]) ON DELETE NO ACTION
);
GO

IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'RoleId', N'CreatedAt', N'Description', N'RoleName') AND [object_id] = OBJECT_ID(N'[Roles]'))
    SET IDENTITY_INSERT [Roles] ON;
INSERT INTO [Roles] ([RoleId], [CreatedAt], [Description], [RoleName])
VALUES ('d7f8a7cf-cf06-4447-9204-ccf3b2ee0001', '2026-01-01T00:00:00.0000000Z', N'System Administrator', N'Admin'),
('d7f8a7cf-cf06-4447-9204-ccf3b2ee0002', '2026-01-01T00:00:00.0000000Z', N'Gym Member', N'Member'),
('d7f8a7cf-cf06-4447-9204-ccf3b2ee0003', '2026-01-01T00:00:00.0000000Z', N'Gym Owner / Partner', N'GymPartner'),
('d7f8a7cf-cf06-4447-9204-ccf3b2ee0004', '2026-01-01T00:00:00.0000000Z', N'Gym Staff member', N'Staff');
IF EXISTS (SELECT * FROM [sys].[identity_columns] WHERE [name] IN (N'RoleId', N'CreatedAt', N'Description', N'RoleName') AND [object_id] = OBJECT_ID(N'[Roles]'))
    SET IDENTITY_INSERT [Roles] OFF;
GO

CREATE UNIQUE INDEX [UQ__MemberPr__1788CC4D526A5F60] ON [MemberProfiles] ([UserId]);
GO

CREATE UNIQUE INDEX [UQ__Roles__8A2B6160ACCD1031] ON [Roles] ([RoleName]);
GO

CREATE INDEX [IX_UserRoles_RoleId] ON [UserRoles] ([RoleId]);
GO

CREATE UNIQUE INDEX [UQ__Users__A9D1053421A327A4] ON [Users] ([Email]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260717175026_InitialIdentity', N'8.0.0');
GO

COMMIT;
GO

BEGIN TRANSACTION;
GO

ALTER TABLE [Users] ADD [GoogleSubject] nvarchar(255) NULL;
GO

CREATE UNIQUE INDEX [UQ_Users_GoogleSubject] ON [Users] ([GoogleSubject]) WHERE [GoogleSubject] IS NOT NULL;
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260717184304_AddGoogleAuthenticationFields', N'8.0.0');
GO

COMMIT;
GO

