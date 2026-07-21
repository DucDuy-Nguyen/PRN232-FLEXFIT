using System.Text;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using FlexFit.Payment.API.Extensions;
using FlexFit.Payment.API.Data;
using FlexFit.Payment.API.Domain.Entities;
using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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

// Add Controllers
builder.Services.AddControllers();

// Register Payment Modules using Extension Methods
builder.Services.AddPaymentApplication();
builder.Services.AddPaymentInfrastructure(builder.Configuration);
builder.Services.AddPaymentAuthentication(builder.Configuration);
builder.Services.AddPaymentSwagger();

// Add Custom Health Checks
builder.Services.AddHealthChecks();

var app = builder.Build();

app.UsePaymentExceptionHandling();

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
                    CreatedAt = DateTime.UtcNow
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
                    CreatedAt = DateTime.UtcNow
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


