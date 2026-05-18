using Flexfit.Helpers;
using Flexfit.Models;
using Flexfit.Repositories;
using Flexfit.Service;
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



// Gym and Branch repositories
builder.Services.AddScoped<IGymRepository, GymRepository>();
builder.Services.AddScoped<IBranchRepository, BranchRepository>();
// Gym and Branch services
builder.Services.AddScoped<IGymService, GymService>();
builder.Services.AddScoped<IBranchService, BranchService>();
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
//Credit repository and service
builder.Services.AddScoped<ICreditRepository, CreditRepository>();
builder.Services.AddScoped<ICreditService, CreditService>();
builder.Services.AddHostedService<BookingReminderWorker>();

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