CREATE DATABASE FlexFitDB;
GO
USE FlexFitDB;
GO

-- ============================================
-- ROLES
-- ============================================

CREATE TABLE Roles (
    RoleId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    RoleName NVARCHAR(50) NOT NULL UNIQUE,
    Description NVARCHAR(255),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
);

-- ============================================
-- USERS (Đã thêm trường DateOfBirth)
-- ============================================

CREATE TABLE Users (
    UserId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) NOT NULL UNIQUE,
    PasswordHash NVARCHAR(MAX) NOT NULL,
    PhoneNumber NVARCHAR(20),
    DateOfBirth DATE NULL, -- <-- THÊM VÀO ĐÂY (Để NULL vì lúc đăng ký nhanh qua Google/Email có thể cập nhật sau)
    AvatarUrl NVARCHAR(MAX),
    IsEmailVerified BIT NOT NULL DEFAULT 0,
    EmailVerificationToken NVARCHAR(MAX) NULL,
    VerificationTokenExpires datetime2 NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    LastLoginAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME2 NULL
);

-- ============================================
-- USER ROLES
-- ============================================

CREATE TABLE UserRoles (
    UserId UNIQUEIDENTIFIER NOT NULL,
    RoleId UNIQUEIDENTIFIER NOT NULL,
    AssignedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_UserRoles PRIMARY KEY (UserId, RoleId),
    CONSTRAINT FK_UserRoles_Users FOREIGN KEY (UserId) REFERENCES Users(UserId),
    CONSTRAINT FK_UserRoles_Roles FOREIGN KEY (RoleId) REFERENCES Roles(RoleId)
);

-- ============================================
-- MEMBER PROFILES (Đã bỏ DateOfBirth vì đã chuyển sang bảng Users)
-- ============================================

CREATE TABLE MemberProfiles (
    MemberProfileId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL UNIQUE,
    Gender NVARCHAR(20),
    HeightCm DECIMAL(5,2),
    WeightKg DECIMAL(5,2),
    FitnessGoal NVARCHAR(255),
    ActivityLevel NVARCHAR(50),
    PreferredWorkoutTime NVARCHAR(50),
    Bio NVARCHAR(MAX),
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

-- ============================================
-- GYMS
-- ============================================

CREATE TABLE Gyms (
    GymId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    OwnerId UNIQUEIDENTIFIER NOT NULL,
    GymName NVARCHAR(150) NOT NULL,
    Description NVARCHAR(MAX),
    ThumbnailUrl NVARCHAR(MAX),
    PhoneNumber NVARCHAR(20),
    Email NVARCHAR(100),
    Status NVARCHAR(30) NOT NULL DEFAULT 'Pending',
    RatingAverage DECIMAL(3,2) NOT NULL DEFAULT 0,
    TotalReviews INT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME2 NULL,
    FOREIGN KEY (OwnerId) REFERENCES Users(UserId)
);

-- ============================================
-- GYM BRANCHES
-- ============================================

CREATE TABLE Branches (
    BranchId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    GymId UNIQUEIDENTIFIER NOT NULL,
    BranchName NVARCHAR(150) NOT NULL,
    Address NVARCHAR(255),
    City NVARCHAR(100),
    District NVARCHAR(100),
    CreditCost INT NOT NULL DEFAULT 0,
    OpenTime TIME,
    CloseTime TIME,
    ThumbnailUrl NVARCHAR(MAX),
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME2 NULL,
    FOREIGN KEY (GymId) REFERENCES Gyms(GymId)
);

-- ============================================
-- BRANCH STAFFS
-- ============================================

CREATE TABLE BranchStaffs (
    StaffId UNIQUEIDENTIFIER NOT NULL,
    BranchId UNIQUEIDENTIFIER NOT NULL,
    AssignedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_BranchStaffs PRIMARY KEY (StaffId, BranchId),
    CONSTRAINT FK_BranchStaffs_Users FOREIGN KEY (StaffId) REFERENCES Users(UserId),
    CONSTRAINT FK_BranchStaffs_Branches FOREIGN KEY (BranchId) REFERENCES Branches(BranchId)
);

-- ============================================
-- GYM IMAGES
-- ============================================

CREATE TABLE GymImages (
    GymImageId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    GymId UNIQUEIDENTIFIER NOT NULL,
    ImageUrl NVARCHAR(MAX) NOT NULL,
    DisplayOrder INT NOT NULL DEFAULT 0,
    FOREIGN KEY (GymId) REFERENCES Gyms(GymId)
);

-- ============================================
-- BRANCH IMAGES
-- ============================================

CREATE TABLE BranchImages (
    BranchImageId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    BranchId UNIQUEIDENTIFIER NOT NULL,
    ImageUrl NVARCHAR(MAX) NOT NULL,
    DisplayOrder INT NOT NULL DEFAULT 0,
    FOREIGN KEY (BranchId) REFERENCES Branches(BranchId)
);

-- ============================================
-- GYM AMENITIES
-- ============================================

CREATE TABLE GymAmenities (
    AmenityId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    AmenityName NVARCHAR(100) NOT NULL UNIQUE
);

-- ============================================
-- BRANCH AMENITY MAPPINGS
-- ============================================

CREATE TABLE BranchAmenityMappings (
    BranchId UNIQUEIDENTIFIER NOT NULL,
    AmenityId UNIQUEIDENTIFIER NOT NULL,
    CONSTRAINT PK_BranchAmenityMappings PRIMARY KEY (BranchId, AmenityId),
    FOREIGN KEY (BranchId) REFERENCES Branches(BranchId),
    FOREIGN KEY (AmenityId) REFERENCES GymAmenities(AmenityId)
);

-- ============================================
-- CATEGORIES
-- ============================================

CREATE TABLE Categories (
    CategoryId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    CategoryName NVARCHAR(100) NOT NULL UNIQUE,
    Description NVARCHAR(255)
);

-- ============================================
-- GYM SESSIONS
-- ============================================

CREATE TABLE GymSessions (
    SessionId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    BranchId UNIQUEIDENTIFIER NOT NULL,
    SessionName NVARCHAR(150),
    StartTime DATETIME2 NOT NULL,
    EndTime DATETIME2 NOT NULL,
    Capacity INT NOT NULL,
    CreditCost INT NOT NULL,
    Status NVARCHAR(30) NOT NULL DEFAULT 'Open',
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME2 NULL,
    FOREIGN KEY (BranchId) REFERENCES Branches(BranchId)
);

-- ============================================
-- CLASSES
-- ============================================

CREATE TABLE Classes (
    ClassId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    BranchId UNIQUEIDENTIFIER NOT NULL,
    CategoryId UNIQUEIDENTIFIER NOT NULL,
    ClassName NVARCHAR(150) NOT NULL,
    Description NVARCHAR(MAX),
    CoachName NVARCHAR(100),
    StartTime DATETIME2 NOT NULL,
    EndTime DATETIME2 NOT NULL,
    Capacity INT NOT NULL,
    CreditCost INT NOT NULL,
    DifficultyLevel NVARCHAR(30),
    CaloriesBurnEstimate INT NULL,
    ThumbnailUrl NVARCHAR(MAX),
    Status NVARCHAR(30) NOT NULL DEFAULT 'Open',
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    UpdatedAt DATETIME2 NULL,
    FOREIGN KEY (BranchId) REFERENCES Branches(BranchId),
    FOREIGN KEY (CategoryId) REFERENCES Categories(CategoryId)
);

-- ============================================
-- CLASS SCHEDULES
-- ============================================

CREATE TABLE ClassSchedules (
    ScheduleId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    ClassId UNIQUEIDENTIFIER NOT NULL,
    DayOfWeek INT NOT NULL,
    StartHour TIME NOT NULL,
    EndHour TIME NOT NULL,
    FOREIGN KEY (ClassId) REFERENCES Classes(ClassId)
);

-- ============================================
-- GYM BOOKINGS
-- ============================================

CREATE TABLE GymBookings (
    BookingId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    SessionId UNIQUEIDENTIFIER NOT NULL,
    BookingCode NVARCHAR(50) NOT NULL UNIQUE,
    CreditUsed INT NOT NULL,
    QrToken NVARCHAR(255),
    QrExpiresAt DATETIME2,
    CheckedInBy UNIQUEIDENTIFIER NULL,
    CheckInStatus NVARCHAR(30) NOT NULL DEFAULT 'NotCheckedIn',
    Status NVARCHAR(30) NOT NULL DEFAULT 'Booked',
    BookedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CancelledAt DATETIME2 NULL,
    RefundCredit INT NOT NULL DEFAULT 0,
    CheckInTime DATETIME2 NULL,
    IsReminded3h bit NOT NULL DEFAULT 0,
    IsReminded1h bit NOT NULL DEFAULT 0,
    FOREIGN KEY (UserId) REFERENCES Users(UserId),
    FOREIGN KEY (SessionId) REFERENCES GymSessions(SessionId),
    FOREIGN KEY (CheckedInBy) REFERENCES Users(UserId)
);

-- ============================================
-- CLASS BOOKINGS
-- ============================================

CREATE TABLE ClassBookings (
    BookingId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    ClassId UNIQUEIDENTIFIER NOT NULL,
    BookingCode NVARCHAR(50) NOT NULL UNIQUE,
    CreditUsed INT NOT NULL,
    QrToken NVARCHAR(255),
    QrExpiresAt DATETIME2,
    CheckedInBy UNIQUEIDENTIFIER NULL,
    CheckInStatus NVARCHAR(30) NOT NULL DEFAULT 'NotCheckedIn',
    Status NVARCHAR(30) NOT NULL DEFAULT 'Booked',
    BookedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CancelledAt DATETIME2 NULL,
    RefundCredit INT NOT NULL DEFAULT 0,
    CheckInTime DATETIME2 NULL,
    IsReminded3h bit NOT NULL DEFAULT 0,
    IsReminded1h bit NOT NULL DEFAULT 0,
    FOREIGN KEY (UserId) REFERENCES Users(UserId),
    FOREIGN KEY (ClassId) REFERENCES Classes(ClassId),
    FOREIGN KEY (CheckedInBy) REFERENCES Users(UserId)
);

-- ============================================
-- CHECKIN LOGS
-- ============================================

CREATE TABLE CheckInLogs (
    CheckInLogId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    GymBookingId UNIQUEIDENTIFIER NULL,
    ClassBookingId UNIQUEIDENTIFIER NULL,
    ScannedBy UNIQUEIDENTIFIER NOT NULL,
    Status NVARCHAR(30) NOT NULL,
    Message NVARCHAR(255),
    ScannedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId),
    FOREIGN KEY (GymBookingId) REFERENCES GymBookings(BookingId),
    FOREIGN KEY (ClassBookingId) REFERENCES ClassBookings(BookingId),
    FOREIGN KEY (ScannedBy) REFERENCES Users(UserId),
    CONSTRAINT CK_CheckInLogs_Target CHECK (
        (GymBookingId IS NOT NULL AND ClassBookingId IS NULL)
        OR
        (GymBookingId IS NULL AND ClassBookingId IS NOT NULL)
    )
);

-- ============================================
-- CREDIT PACKAGES
-- ============================================

CREATE TABLE CreditPackages (
    PackageId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    PackageName NVARCHAR(100) NOT NULL,
    CreditAmount INT NOT NULL,
    BonusCredit INT NOT NULL DEFAULT 0,
    Price DECIMAL(18,2) NOT NULL,
    Description NVARCHAR(MAX),
    IsPopular BIT NOT NULL DEFAULT 0,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
);

-- ============================================
-- USER CREDITS
-- ============================================

CREATE TABLE UserCredits (
    UserCreditId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL UNIQUE,
    Balance INT NOT NULL DEFAULT 0,
    TotalEarned INT NOT NULL DEFAULT 0,
    TotalSpent INT NOT NULL DEFAULT 0,
    UpdatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

-- ============================================
-- CREDIT TRANSACTIONS
-- ============================================

CREATE TABLE CreditTransactions (
    TransactionId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    Amount INT NOT NULL,
    BalanceBefore INT NOT NULL,
    BalanceAfter INT NOT NULL,
    Type NVARCHAR(30) NOT NULL,
    ReferenceId UNIQUEIDENTIFIER NULL,
    ReferenceType NVARCHAR(30) NULL,
    Description NVARCHAR(255),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);
-- ============================================
-- PROMOTIONS
-- ============================================

CREATE TABLE Promotions (
    PromotionId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    Title NVARCHAR(150) NOT NULL,
    Description NVARCHAR(MAX),
    DiscountPercent INT,
    StartDate DATETIME2 NOT NULL,
    EndDate DATETIME2 NOT NULL,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE()
);

-- ============================================
-- PAYMENTS
-- ============================================

CREATE TABLE Payments (
    PaymentId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    PackageId UNIQUEIDENTIFIER NOT NULL,
    Amount DECIMAL(18,2) NOT NULL,
    PaymentMethod NVARCHAR(50),
    ProviderTransactionCode NVARCHAR(100),
    Status NVARCHAR(30) NOT NULL DEFAULT 'Pending',
    PaidAt DATETIME2 NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    PromotionId UNIQUEIDENTIFIER NULL,
    DiscountAmount DECIMAL(18,2) NOT NULL DEFAULT 0,
    FOREIGN KEY (UserId) REFERENCES Users(UserId),
    FOREIGN KEY (PackageId) REFERENCES CreditPackages(PackageId),
    CONSTRAINT FK_Payments_Promotions 
FOREIGN KEY (PromotionId) REFERENCES Promotions(PromotionId)
);

-- ============================================
-- REVIEWS
-- ============================================

CREATE TABLE Reviews (
    ReviewId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    GymId UNIQUEIDENTIFIER NULL,
    ClassId UNIQUEIDENTIFIER NULL,
    Rating INT NOT NULL,
    Comment NVARCHAR(MAX) NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    ClassBookingId UNIQUEIDENTIFIER NULL,
    GymBookingId UNIQUEIDENTIFIER NULL,

    FOREIGN KEY (UserId) REFERENCES Users(UserId),
    FOREIGN KEY (GymId) REFERENCES Gyms(GymId),
    FOREIGN KEY (ClassId) REFERENCES Classes(ClassId),
    FOREIGN KEY (ClassBookingId) REFERENCES ClassBookings(BookingId),
    FOREIGN KEY (GymBookingId) REFERENCES GymBookings(BookingId),

    CONSTRAINT CK_Reviews_Target CHECK (
        (GymId IS NOT NULL AND ClassId IS NULL)
        OR
        (GymId IS NULL AND ClassId IS NOT NULL)
    ),

    CONSTRAINT CK_Reviews_Booking_Target CHECK (
        (ClassBookingId IS NOT NULL AND GymBookingId IS NULL)
        OR
        (ClassBookingId IS NULL AND GymBookingId IS NOT NULL)
    ),

    CONSTRAINT CK_Reviews_Rating CHECK (Rating BETWEEN 1 AND 5)
);
GO

-- ============================================
-- FAVORITE GYMS
-- ============================================

CREATE TABLE FavoriteGyms (
    UserId UNIQUEIDENTIFIER NOT NULL,
    GymId UNIQUEIDENTIFIER NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    CONSTRAINT PK_FavoriteGyms PRIMARY KEY (UserId, GymId),
    FOREIGN KEY (UserId) REFERENCES Users(UserId),
    FOREIGN KEY (GymId) REFERENCES Gyms(GymId)
);
CREATE TABLE FavoriteClasses (
    UserId UNIQUEIDENTIFIER NOT NULL,
    ClassId UNIQUEIDENTIFIER NOT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    
    -- Khóa chính hỗn hợp (Một user chỉ thích một class duy nhất 1 lần)
    CONSTRAINT PK_FavoriteClasses PRIMARY KEY (UserId, ClassId),
    
    -- Khóa ngoại nối đến bảng Users và Classes
    FOREIGN KEY (UserId) REFERENCES Users(UserId) ON DELETE CASCADE,
    FOREIGN KEY (ClassId) REFERENCES Classes(ClassId) ON DELETE CASCADE
);

-- ============================================
-- NOTIFICATIONS
-- ============================================

CREATE TABLE Notifications (
    NotificationId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    Title NVARCHAR(150) NOT NULL,
    Content NVARCHAR(MAX) NOT NULL,
    Type NVARCHAR(50),
    IsRead BIT NOT NULL DEFAULT 0,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);



-- ============================================
-- WORKOUT HISTORIES
-- ============================================

CREATE TABLE UserWorkoutHistories (
    WorkoutHistoryId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NOT NULL,
    GymBookingId UNIQUEIDENTIFIER NULL,
    ClassBookingId UNIQUEIDENTIFIER NULL,
    CaloriesBurned INT NULL,
    WorkoutDurationMinutes INT NULL,
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId),
    FOREIGN KEY (GymBookingId) REFERENCES GymBookings(BookingId),
    FOREIGN KEY (ClassBookingId) REFERENCES ClassBookings(BookingId),
    CONSTRAINT CK_UserWorkoutHistories_Target CHECK (
        (GymBookingId IS NOT NULL AND ClassBookingId IS NULL)
        OR
        (GymBookingId IS NULL AND ClassBookingId IS NOT NULL)
    )
);

-- ============================================
-- SYSTEM LOGS
-- ============================================

CREATE TABLE SystemLogs (
    LogId UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId UNIQUEIDENTIFIER NULL,
    Action NVARCHAR(255),
    Description NVARCHAR(MAX),
    IpAddress NVARCHAR(50),
    CreatedAt DATETIME2 NOT NULL DEFAULT GETDATE(),
    FOREIGN KEY (UserId) REFERENCES Users(UserId)
);

-- ============================================
-- SEED DATA
-- ============================================

INSERT INTO Roles (RoleName, Description)
VALUES
('Member', N'Người dùng tập luyện / Hội viên'),
('Staff', N'Nhân viên trực chi nhánh phòng tập'),
('GymPartner', N'Chủ chuỗi phòng gym / Đối tác'),
('Manager', N'Quản lý nền tảng / Quản lý vận hành'),
('Admin', N'Quản trị cấp cao toàn hệ thống');

INSERT INTO Categories (CategoryName, Description)
VALUES
(N'Yoga', N'Lớp yoga'),
(N'Pilates', N'Lớp pilates'),
(N'Boxing', N'Lớp boxing'),
(N'Kickboxing', N'Lớp kickboxing'),
(N'Dance', N'Lớp nhảy'),
(N'Crossfit', N'Lớp crossfit'),
(N'HIIT', N'Lớp cường độ cao'),
(N'Zumba', N'Lớp zumba');

INSERT INTO GymAmenities (AmenityName)
VALUES
(N'Parking'),
(N'Shower'),
(N'Locker'),
(N'AirConditioner'),
(N'Wifi'),
(N'ProteinBar'),
(N'Towel'),
(N'PersonalTrainer');

INSERT INTO CreditPackages
(PackageName, CreditAmount, BonusCredit, Price, Description, IsPopular, IsActive)
VALUES
(N'Gói Starter', 50, 0, 199000, N'Phù hợp người mới bắt đầu', 0, 1),
(N'Gói Standard', 120, 10, 399000, N'Gói phổ biến cho người tập đều', 1, 1),
(N'Gói Premium', 300, 50, 899000, N'Gói tiết kiệm cho người tập nhiều', 0, 1);
GO