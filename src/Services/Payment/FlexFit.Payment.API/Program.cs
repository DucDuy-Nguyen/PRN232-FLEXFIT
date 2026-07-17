using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using FlexFit.Payment.Application.Interfaces;
using FlexFit.Payment.Application.Services;
using FlexFit.Payment.Infrastructure.Data;
using FlexFit.Payment.Infrastructure.Repositories;
using FlexFit.Payment.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PayOS;
using StackExchange.Redis;

var builder = WebApplication.CreateBuilder(args);

// Add CORS
var frontendUrl = builder.Configuration["Frontend:FrontendUrl"] ?? "http://localhost:5173";
builder.Services.AddCors(options =>
{
    options.AddPolicy("CorsPolicy", policy =>
    {
        policy.WithOrigins(frontendUrl)
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

// ==============================
// 1️⃣ Add Services
// ==============================

// Add DbContext (SQL Server)
builder.Services.AddDbContext<PaymentDbContext>(options =>
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
    var key = Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"] ?? "VeryLongSuperSecureKey1234567890!!");
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "FlexFitAPI",
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "FlexFitClient",
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

// Register Redis Connection Multiplexer
builder.Services.AddSingleton<IConnectionMultiplexer>(sp =>
{
    var connString = builder.Configuration["Redis:ConnectionString"] ?? "localhost:6379,abortConnect=false";
    var options = ConfigurationOptions.Parse(connString);
    options.AbortOnConnectFail = false;
    return ConnectionMultiplexer.Connect(options);
});

// Register PayOS Client
builder.Services.AddSingleton(sp =>
{
    var payOsSettings = builder.Configuration.GetSection("PayOS");
    return new PayOSClient(
        payOsSettings["ClientId"] ?? "",
        payOsSettings["ApiKey"] ?? "",
        payOsSettings["ChecksumKey"] ?? ""
    );
});

// Register Dependencies (DI)
builder.Services.AddScoped<IPaymentRepository, PaymentRepository>();
builder.Services.AddScoped<ICreditRepository, CreditRepository>();
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddScoped<IProcessedMessageRepository, ProcessedMessageRepository>();

builder.Services.AddScoped<IDistributedLockService, RedisDistributedLockService>();
builder.Services.AddScoped<ICacheService, RedisCacheService>();
builder.Services.AddScoped<IIdempotencyService, RedisIdempotencyService>();
builder.Services.AddScoped<IEventPublisher, RedisEventPublisher>();
builder.Services.AddScoped<IPayOSPaymentGateway, PayOSPaymentGateway>();

builder.Services.AddScoped<IPaymentService, PaymentService>();
builder.Services.AddScoped<ICreditService, CreditService>();
builder.Services.AddScoped<ICreditAdjustmentService, CreditAdjustmentService>();

// Add Custom Health Checks
builder.Services.AddHealthChecks();

// ==============================
// 2️⃣ Build app
// ==============================
var app = builder.Build();

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("CorsPolicy");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health Check endpoint
app.MapGet("/health", async (PaymentDbContext db, IConnectionMultiplexer redis) =>
{
    var dbConnected = false;
    var redisConnected = false;
    try
    {
        dbConnected = await db.Database.CanConnectAsync();
    }
    catch (Exception)
    {
        dbConnected = false;
    }

    try
    {
        if (redis.IsConnected)
        {
            var redisDb = redis.GetDatabase();
            await redisDb.PingAsync();
            redisConnected = true;
        }
    }
    catch (Exception)
    {
        redisConnected = false;
    }

    if (!dbConnected)
    {
        return Results.StatusCode(500);
    }

    if (!redisConnected)
    {
        return Results.Ok(new { Status = "Degraded", Database = "Connected", Redis = "Disconnected" });
    }

    return Results.Ok(new { Status = "Healthy", Database = "Connected", Redis = "Connected" });
});

// Seed Credit Packages if empty
using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<PaymentDbContext>();
    try
    {
        if (!context.CreditPackages.Any())
        {
            context.CreditPackages.AddRange(
                new FlexFit.Payment.Domain.Entities.CreditPackage
                {
                    PackageId = Guid.NewGuid(),
                    PackageName = "Gói Đồng (Bronze)",
                    CreditAmount = 100,
                    BonusCredit = 0,
                    Price = 100000,
                    Description = "Gói tín dụng cơ bản cho người mới bắt đầu.",
                    IsPopular = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new FlexFit.Payment.Domain.Entities.CreditPackage
                {
                    PackageId = Guid.NewGuid(),
                    PackageName = "Gói Bạc (Silver)",
                    CreditAmount = 500,
                    BonusCredit = 50,
                    Price = 500000,
                    Description = "Nhận thêm 50 tín dụng bonus cực hấp dẫn.",
                    IsPopular = true,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                },
                new FlexFit.Payment.Domain.Entities.CreditPackage
                {
                    PackageId = Guid.NewGuid(),
                    PackageName = "Gói Vàng (Gold)",
                    CreditAmount = 1000,
                    BonusCredit = 150,
                    Price = 1000000,
                    Description = "Tiết kiệm tối đa, nhận thêm 150 tín dụng bonus!",
                    IsPopular = false,
                    IsActive = true,
                    CreatedAt = DateTime.UtcNow
                }
            );
            context.SaveChanges();
        }
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Error seeding credit packages: {ex.Message}");
    }
}
if (app.Environment.IsDevelopment())
{
    app.MapPost("/dev/token", (DevTokenRequest request, IConfiguration configuration) =>
    {
        var jwtSettings = configuration.GetSection("Jwt");
        var keyStr = jwtSettings["Key"] ?? "VeryLongSuperSecureKey1234567890!!";
        var issuer = jwtSettings["Issuer"] ?? "FlexFitAPI";
        var audience = jwtSettings["Audience"] ?? "FlexFitClient";
        var expiresMinutes = int.TryParse(jwtSettings["ExpiresInMinutes"], out var min) ? min : 60;

        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyStr));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        
        var expires = DateTime.UtcNow.AddMinutes(expiresMinutes);

        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, request.UserId.ToString()),
            new Claim("userId", request.UserId.ToString()),
            new Claim(ClaimTypes.NameIdentifier, request.UserId.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, request.Email),
            new Claim(ClaimTypes.Email, request.Email),
            new Claim("role", request.Role),
            new Claim(ClaimTypes.Role, request.Role)
        };

        var token = new JwtSecurityToken(
            issuer: issuer,
            audience: audience,
            claims: claims,
            expires: expires,
            signingCredentials: creds
        );

        var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

        return Results.Ok(new
        {
            accessToken = tokenString,
            expiresAt = expires.ToString("o")
        });
    })
    .WithName("GenerateDevToken")
    .WithTags("Development Testing Only");
}

app.Run();

public class DevTokenRequest
{
    public Guid UserId { get; set; }
    public string Email { get; set; } = null!;
    public string Role { get; set; } = null!;
}
