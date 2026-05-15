using Flexfit.Helpers;
using Flexfit.Models;
using Flexfit.Service;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Flexfit.Repositories;



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
builder.Services.AddSwaggerGen();

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

// Register Services (DI)
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<AuthService>();
//builder.Services.AddScoped<UserService>();
//builder.Services.AddScoped<BookingService>();
//builder.Services.AddScoped<CreditService>();
builder.Services.AddSingleton<JwtHelper>();

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

app.UseHttpsRedirection();

// Authentication & Authorization
app.UseAuthentication();
app.UseAuthorization();

// Map controllers
app.MapControllers();

// ==============================
// 4️⃣ Run app
// ==============================

app.Run();