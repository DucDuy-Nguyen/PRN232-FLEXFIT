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

CREATE TABLE [ClassBookings] (
    [BookingId] uniqueidentifier NOT NULL DEFAULT ((newid())),
    [UserId] uniqueidentifier NOT NULL,
    [ClassId] uniqueidentifier NOT NULL,
    [ScheduleId] uniqueidentifier NULL,
    [BranchId] uniqueidentifier NOT NULL,
    [GymId] uniqueidentifier NOT NULL,
    [BookingCode] nvarchar(50) NOT NULL,
    [CreditUsed] int NOT NULL,
    [GymNameSnapshot] nvarchar(150) NOT NULL,
    [ClassNameSnapshot] nvarchar(150) NOT NULL,
    [BranchNameSnapshot] nvarchar(150) NOT NULL,
    [BranchAddressSnapshot] nvarchar(255) NOT NULL,
    [CoachNameSnapshot] nvarchar(100) NOT NULL,
    [StartTimeSnapshot] datetime2 NOT NULL,
    [EndTimeSnapshot] datetime2 NOT NULL,
    [QrToken] nvarchar(255) NULL,
    [QrExpiresAt] datetime2 NULL,
    [CheckedInBy] uniqueidentifier NULL,
    [CheckInStatus] nvarchar(30) NOT NULL DEFAULT N'NotCheckedIn',
    [Status] nvarchar(30) NOT NULL DEFAULT N'Booked',
    [BookedAt] datetime2 NOT NULL DEFAULT ((getutcdate())),
    [CancelledAt] datetime2 NULL,
    [ExpiredAt] datetime2 NULL,
    [CheckInTime] datetime2 NULL,
    [RefundCredit] int NOT NULL,
    [IsReminded3h] bit NOT NULL,
    [IsReminded1h] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT ((getutcdate())),
    [UpdatedAt] datetime2 NOT NULL DEFAULT ((getutcdate())),
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_ClassBookings] PRIMARY KEY ([BookingId])
);
GO

CREATE TABLE [GymBookings] (
    [BookingId] uniqueidentifier NOT NULL DEFAULT ((newid())),
    [UserId] uniqueidentifier NOT NULL,
    [SessionId] uniqueidentifier NOT NULL,
    [BranchId] uniqueidentifier NOT NULL,
    [GymId] uniqueidentifier NOT NULL,
    [BookingCode] nvarchar(50) NOT NULL,
    [CreditUsed] int NOT NULL,
    [GymNameSnapshot] nvarchar(150) NOT NULL,
    [SessionNameSnapshot] nvarchar(150) NOT NULL,
    [BranchNameSnapshot] nvarchar(150) NOT NULL,
    [BranchAddressSnapshot] nvarchar(255) NOT NULL,
    [StartTimeSnapshot] datetime2 NOT NULL,
    [EndTimeSnapshot] datetime2 NOT NULL,
    [QrToken] nvarchar(255) NULL,
    [QrExpiresAt] datetime2 NULL,
    [CheckedInBy] uniqueidentifier NULL,
    [CheckInStatus] nvarchar(30) NOT NULL DEFAULT N'NotCheckedIn',
    [Status] nvarchar(30) NOT NULL DEFAULT N'Booked',
    [BookedAt] datetime2 NOT NULL DEFAULT ((getutcdate())),
    [CancelledAt] datetime2 NULL,
    [ExpiredAt] datetime2 NULL,
    [CheckInTime] datetime2 NULL,
    [RefundCredit] int NOT NULL,
    [IsReminded3h] bit NOT NULL,
    [IsReminded1h] bit NOT NULL,
    [CreatedAt] datetime2 NOT NULL DEFAULT ((getutcdate())),
    [UpdatedAt] datetime2 NOT NULL DEFAULT ((getutcdate())),
    [RowVersion] rowversion NOT NULL,
    CONSTRAINT [PK_GymBookings] PRIMARY KEY ([BookingId])
);
GO

CREATE TABLE [InboxMessages] (
    [EventId] uniqueidentifier NOT NULL,
    [EventType] nvarchar(150) NOT NULL,
    [ConsumerName] nvarchar(255) NOT NULL,
    [ReceivedAt] datetime2 NOT NULL,
    [ProcessedAt] datetime2 NULL,
    [ErrorMessage] nvarchar(max) NULL,
    CONSTRAINT [PK_InboxMessages] PRIMARY KEY ([EventId])
);
GO

CREATE TABLE [OutboxMessages] (
    [OutboxMessageId] uniqueidentifier NOT NULL DEFAULT ((newid())),
    [EventType] nvarchar(150) NOT NULL,
    [AggregateType] nvarchar(150) NOT NULL,
    [AggregateId] uniqueidentifier NOT NULL,
    [Payload] nvarchar(max) NOT NULL,
    [CorrelationId] nvarchar(150) NULL,
    [OccurredAt] datetime2 NOT NULL,
    [ProcessedAt] datetime2 NULL,
    [RetryCount] int NOT NULL,
    [ErrorMessage] nvarchar(max) NULL,
    CONSTRAINT [PK_OutboxMessages] PRIMARY KEY ([OutboxMessageId])
);
GO

CREATE TABLE [CheckInLogs] (
    [CheckInLogId] uniqueidentifier NOT NULL DEFAULT ((newid())),
    [UserId] uniqueidentifier NOT NULL,
    [GymBookingId] uniqueidentifier NULL,
    [ClassBookingId] uniqueidentifier NULL,
    [ScannedBy] uniqueidentifier NOT NULL,
    [Status] nvarchar(30) NOT NULL,
    [Message] nvarchar(255) NULL,
    [ScannedAt] datetime2 NOT NULL DEFAULT ((getutcdate())),
    [CreatedAt] datetime2 NOT NULL DEFAULT ((getutcdate())),
    CONSTRAINT [PK_CheckInLogs] PRIMARY KEY ([CheckInLogId]),
    CONSTRAINT [FK_CheckInLogs_ClassBookings_ClassBookingId] FOREIGN KEY ([ClassBookingId]) REFERENCES [ClassBookings] ([BookingId]) ON DELETE CASCADE,
    CONSTRAINT [FK_CheckInLogs_GymBookings_GymBookingId] FOREIGN KEY ([GymBookingId]) REFERENCES [GymBookings] ([BookingId]) ON DELETE CASCADE
);
GO

CREATE INDEX [IX_CheckInLogs_ClassBookingId] ON [CheckInLogs] ([ClassBookingId]);
GO

CREATE INDEX [IX_CheckInLogs_GymBookingId] ON [CheckInLogs] ([GymBookingId]);
GO

CREATE UNIQUE INDEX [IX_ClassBookings_BookingCode] ON [ClassBookings] ([BookingCode]);
GO

CREATE UNIQUE INDEX [IX_GymBookings_BookingCode] ON [GymBookings] ([BookingCode]);
GO

CREATE INDEX [IX_OutboxMessages_ProcessedAt] ON [OutboxMessages] ([ProcessedAt]);
GO

INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
VALUES (N'20260717134718_InitialCreate', N'8.0.0');
GO

COMMIT;
GO

