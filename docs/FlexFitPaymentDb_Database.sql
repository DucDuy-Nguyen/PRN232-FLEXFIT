IF DB_ID('FlexFitPaymentDb') IS NULL
BEGIN
    CREATE DATABASE [FlexFitPaymentDb];
END
GO

USE [FlexFitPaymentDb];
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

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CreditPackages')
BEGIN
    CREATE TABLE [CreditPackages] (
        [PackageId] uniqueidentifier NOT NULL,
        [PackageName] nvarchar(100) NOT NULL,
        [CreditAmount] int NOT NULL,
        [BonusCredit] int NOT NULL,
        [Price] decimal(18,2) NOT NULL,
        [Description] nvarchar(max) NULL,
        [IsPopular] bit NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CreditPackages] PRIMARY KEY ([PackageId])
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'CreditTransactions')
BEGIN
    CREATE TABLE [CreditTransactions] (
        [TransactionId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Amount] int NOT NULL,
        [BalanceBefore] int NOT NULL,
        [BalanceAfter] int NOT NULL,
        [Type] nvarchar(30) NOT NULL,
        [ReferenceId] uniqueidentifier NULL,
        [ReferenceType] nvarchar(30) NULL,
        [Description] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_CreditTransactions] PRIMARY KEY ([TransactionId])
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'OutboxMessages')
BEGIN
    CREATE TABLE [OutboxMessages] (
        [Id] uniqueidentifier NOT NULL,
        [EventType] nvarchar(100) NOT NULL,
        [Payload] nvarchar(max) NOT NULL,
        [OccurredAt] datetime2 NOT NULL,
        [ProcessedAt] datetime2 NULL,
        [Error] nvarchar(max) NULL,
        CONSTRAINT [PK_OutboxMessages] PRIMARY KEY ([Id])
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'ProcessedMessages')
BEGIN
    CREATE TABLE [ProcessedMessages] (
        [MessageId] uniqueidentifier NOT NULL,
        [ProcessedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_ProcessedMessages] PRIMARY KEY ([MessageId])
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'UserCredits')
BEGIN
    CREATE TABLE [UserCredits] (
        [UserCreditId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [Balance] int NOT NULL,
        [TotalEarned] int NOT NULL,
        [TotalSpent] int NOT NULL,
        [UpdatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_UserCredits] PRIMARY KEY ([UserCreditId])
    );
END
GO

IF NOT EXISTS (SELECT * FROM sys.tables WHERE name = 'Payments')
BEGIN
    CREATE TABLE [Payments] (
        [PaymentId] uniqueidentifier NOT NULL,
        [UserId] uniqueidentifier NOT NULL,
        [PackageId] uniqueidentifier NOT NULL,
        [Amount] decimal(18,2) NOT NULL,
        [PaymentMethod] nvarchar(50) NULL,
        [ProviderTransactionCode] nvarchar(100) NULL,
        [Status] nvarchar(30) NOT NULL,
        [PaidAt] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        CONSTRAINT [PK_Payments] PRIMARY KEY ([PaymentId]),
        CONSTRAINT [FK_Payments_CreditPackages_PackageId] FOREIGN KEY ([PackageId]) REFERENCES [CreditPackages] ([PackageId])
    );
END
GO

IF NOT EXISTS(SELECT * FROM sys.indexes WHERE name = 'IX_CreditTransactions_UserId_CreatedAt' AND object_id = OBJECT_ID('CreditTransactions'))
    CREATE INDEX [IX_CreditTransactions_UserId_CreatedAt] ON [CreditTransactions] ([UserId], [CreatedAt]);
GO

IF NOT EXISTS(SELECT * FROM sys.indexes WHERE name = 'IX_OutboxMessages_ProcessedAt' AND object_id = OBJECT_ID('OutboxMessages'))
    CREATE INDEX [IX_OutboxMessages_ProcessedAt] ON [OutboxMessages] ([ProcessedAt]);
GO

IF NOT EXISTS(SELECT * FROM sys.indexes WHERE name = 'IX_Payments_PackageId' AND object_id = OBJECT_ID('Payments'))
    CREATE INDEX [IX_Payments_PackageId] ON [Payments] ([PackageId]);
GO

IF NOT EXISTS(SELECT * FROM sys.indexes WHERE name = 'IX_Payments_Status' AND object_id = OBJECT_ID('Payments'))
    CREATE INDEX [IX_Payments_Status] ON [Payments] ([Status]);
GO

IF NOT EXISTS(SELECT * FROM sys.indexes WHERE name = 'IX_Payments_UserId_CreatedAt' AND object_id = OBJECT_ID('Payments'))
    CREATE INDEX [IX_Payments_UserId_CreatedAt] ON [Payments] ([UserId], [CreatedAt]);
GO

IF NOT EXISTS(SELECT * FROM sys.indexes WHERE name = 'IX_UserCredits_UserId' AND object_id = OBJECT_ID('UserCredits'))
    CREATE UNIQUE INDEX [IX_UserCredits_UserId] ON [UserCredits] ([UserId]);
GO

IF NOT EXISTS(SELECT * FROM [__EFMigrationsHistory] WHERE [MigrationId] = N'20260717135514_InitialCreate')
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260717135514_InitialCreate', N'8.0.0');
END;
GO

COMMIT;
GO

-- Seed Data cho CreditPackages
IF NOT EXISTS(SELECT * FROM [CreditPackages] WHERE [PackageId] = '11111111-1111-1111-1111-111111111111')
BEGIN
    INSERT INTO [CreditPackages] ([PackageId], [PackageName], [CreditAmount], [BonusCredit], [Price], [Description], [IsPopular], [IsActive], [CreatedAt])
    VALUES ('11111111-1111-1111-1111-111111111111', N'Gói Cơ Bản', 100, 0, 100000.00, N'Gói 100 Credit cơ bản', 0, 1, GETUTCDATE());
END;
GO

IF NOT EXISTS(SELECT * FROM [CreditPackages] WHERE [PackageId] = '22222222-2222-2222-2222-222222222222')
BEGIN
    INSERT INTO [CreditPackages] ([PackageId], [PackageName], [CreditAmount], [BonusCredit], [Price], [Description], [IsPopular], [IsActive], [CreatedAt])
    VALUES ('22222222-2222-2222-2222-222222222222', N'Gói Phổ Biến', 500, 50, 500000.00, N'Gói 500 Credit + 50 Credit thưởng', 1, 1, GETUTCDATE());
END;
GO

IF NOT EXISTS(SELECT * FROM [CreditPackages] WHERE [PackageId] = '33333333-3333-3333-3333-333333333333')
BEGIN
    INSERT INTO [CreditPackages] ([PackageId], [PackageName], [CreditAmount], [BonusCredit], [Price], [Description], [IsPopular], [IsActive], [CreatedAt])
    VALUES ('33333333-3333-3333-3333-333333333333', N'Gói Cao Cấp', 1000, 150, 1000000.00, N'Gói 1000 Credit + 150 Credit thưởng', 0, 1, GETUTCDATE());
END;
GO
