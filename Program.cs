using Flexfit.Helpers;
using Flexfit.Models;
using Flexfit.Repositories;
using Flexfit.Service;
using PayOS;
using Flexfit.Services;

using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ==============================
// 1️⃣ Add Services
// ==============================

// Add DbContext (SQL Server)
builder.Services.AddDbContext<FlexFitDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Controllers
builder.Services.AddControllers();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Vui lòng nhập token theo định dạng: Bearer {token_của_bạn}",
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            new string[] { }
        }
    });
});

// Add Authentication (JWT)
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]);
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"],
        ValidAudience = builder.Configuration["Jwt:Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

// Add Authorization (Role-based)
builder.Services.AddAuthorization();

// Register Repositories (DI)
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IEmailService, EmailService>();
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();



// Gym and Branch repositories
builder.Services.AddScoped<IGymRepository, GymRepository>();
builder.Services.AddScoped<IBranchRepository, BranchRepository>();
builder.Services.AddScoped<IClassRepository, ClassRepository>();
// Gym and Branch services
builder.Services.AddScoped<IGymService, GymService>();
builder.Services.AddScoped<IBranchService, BranchService>();
builder.Services.AddScoped<IClassService, ClassService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
//builder.Services.AddScoped<UserService>();
builder.Services.AddScoped<IUserService, UserService>();
//builder.Services.AddScoped<BookingService>();
//builder.Services.AddScoped<CreditService>();

builder.Services.AddScoped<IAuthService, AuthService>();


// Gym and Branch repository
builder.Services.AddScoped<IGymRepository, GymRepository>();
builder.Services.AddScoped<IBranchRepository, BranchRepository>();
// Register Services (DI)
builder.Services.AddScoped<IAuthService, AuthService>();
//Booking repository and service
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<IPaymentService, PaymentService>();

// Member profile repository and service
builder.Services.AddScoped<IMemberProfileRepository, MemberProfileRepository>();
builder.Services.AddScoped<IMemberProfileService, MemberProfileService>();
//Credit repository and service
builder.Services.AddScoped<ICreditRepository, CreditRepository>();
builder.Services.AddScoped<ICreditService, CreditService>();
// check-in log repository and service
builder.Services.AddScoped<ICheckInLogRepository, CheckInLogRepository>();
builder.Services.AddScoped<ICheckInLogService, CheckInLogService>();
// Đăng ký cặp Repository xử lý Database của FavoriteGym
builder.Services.AddScoped<IFavoriteGymRepository, FavoriteGymRepository>();

// Đăng ký cặp Service xử lý nghiệp vụ Toggle của FavoriteGym
builder.Services.AddScoped<IFavoriteGymService, FavoriteGymService>();
// Đăng ký cho cụm Favorite Class mới tạo
builder.Services.AddScoped<IFavoriteClassRepository, FavoriteClassRepository>();
builder.Services.AddScoped<IFavoriteClassService, FavoriteClassService>();
// Review service - Đánh giá lịch đặt (chỉ được đánh giá sau khi Check-in & 1 lần/booking)
builder.Services.AddScoped<IReviewService, ReviewService>();

// Workout History service and repository
builder.Services.AddScoped<IWorkoutHistoryRepository, WorkoutHistoryRepository>();
builder.Services.AddScoped<IWorkoutHistoryService, WorkoutHistoryService>();

builder.Services.AddHostedService<BookingReminderWorker>();



builder.Services.AddSingleton<JwtHelper>();

// Register PayOS Client
var payOsSettings = builder.Configuration.GetSection("PayOS");
builder.Services.AddSingleton(new PayOSClient(
    payOsSettings["ClientId"] ?? "",
    payOsSettings["ApiKey"] ?? "",
    payOsSettings["ChecksumKey"] ?? ""
));

// ==============================
// 2️⃣ Build app
// ==============================

var app = builder.Build();

// ==============================
// 3️⃣ Configure Middleware
// ==============================

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

//app.UseHttpsRedirection();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// Seed Credit Packages if empty
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<FlexFitDbContext>();
    try
    {
        if (!context.CreditPackages.Any())
        {
            context.CreditPackages.AddRange(
                new CreditPackage
                {
                    PackageId = Guid.NewGuid(),
                    PackageName = "Gói Đồng (Bronze)",
                    CreditAmount = 100,
                    BonusCredit = 0,
                    Price = 100000,
                    Description = "Gói tín dụng cơ bản cho người mới bắt đầu.",
                    IsPopular = false,
                    IsActive = true,
                    CreatedAt = DateTimeHelper.GetVietnamTime()
                },
                new CreditPackage
                {
                    PackageId = Guid.NewGuid(),
                    PackageName = "Gói Bạc (Silver)",
                    CreditAmount = 500,
                    BonusCredit = 50,
                    Price = 500000,
                    Description = "Nhận thêm 50 tín dụng bonus cực hấp dẫn.",
                    IsPopular = true,
                    IsActive = true,
                    CreatedAt = DateTimeHelper.GetVietnamTime()
                },
                new CreditPackage
                {
                    PackageId = Guid.NewGuid(),
                    PackageName = "Gói Vàng (Gold)",
                    CreditAmount = 1000,
                    BonusCredit = 150,
                    Price = 1000000,
                    Description = "Tiết kiệm tối đa, nhận thêm 150 tín dụng bonus!",
                    IsPopular = false,
                    IsActive = true,
                    CreatedAt = DateTimeHelper.GetVietnamTime()
                }
            );
            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        // Log error or ignore if database is not created yet
        Console.WriteLine($"Error seeding credit packages: {ex.Message}");
    }
}

app.Run();