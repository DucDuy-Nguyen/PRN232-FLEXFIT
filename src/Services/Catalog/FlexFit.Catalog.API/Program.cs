using FlexFit.Catalog.Repository.Data;
using FlexFit.Catalog.Repository.Models;
using FlexFit.Catalog.Repository.Repositories;
using FlexFit.Catalog.Service.Interfaces;
using FlexFit.Catalog.Service.Services;
using FlexFit.Catalog.API.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System.Text;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

// ==============================
// 1️⃣ Configuration
// ==============================
var connectionString = builder.Configuration["CATALOG_DB_CONNECTION_STRING"] 
                       ?? builder.Configuration.GetConnectionString("DefaultConnection");

// Add DbContext
builder.Services.AddDbContext<CatalogDbContext>(options =>
    options.UseSqlServer(connectionString));

// Add Controllers
builder.Services.AddControllers();

// Add Swagger
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FlexFit Catalog Service API", Version = "v1" });
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        In = ParameterLocation.Header,
        Description = "Vui lòng nhập token theo định dạng: Bearer {token_của_bạn}",
        Name = "Authorization",
        Type = SecuritySchemeType.ApiKey
    });
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
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
    var jwtKey = builder.Configuration["JWT_KEY"] ?? builder.Configuration["Jwt:Key"] ?? "FlexFitSuperSecretKeyOfAtLeast32BytesLength!";
    var jwtIssuer = builder.Configuration["JWT_ISSUER"] ?? builder.Configuration["Jwt:Issuer"] ?? "FlexFit";
    var jwtAudience = builder.Configuration["JWT_AUDIENCE"] ?? builder.Configuration["Jwt:Audience"] ?? "FlexFitClients";

    var key = Encoding.UTF8.GetBytes(jwtKey);
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(key)
    };
});

builder.Services.AddAuthorization();
builder.Services.AddHttpContextAccessor();

// Health Checks
builder.Services.AddHealthChecks()
    .AddDbContextCheck<CatalogDbContext>();

// Add gRPC
builder.Services.AddGrpc();

// Register DI Services
builder.Services.AddSingleton<IRedisPublisher, RedisPublisher>();
builder.Services.AddScoped(typeof(IGenericRepository<>), typeof(GenericRepository<>));
builder.Services.AddScoped<IGymRepository, GymRepository>();
builder.Services.AddScoped<IBranchRepository, BranchRepository>();
builder.Services.AddScoped<IClassRepository, ClassRepository>();
builder.Services.AddScoped<IFavoriteGymRepository, FavoriteGymRepository>();
builder.Services.AddScoped<IFavoriteClassRepository, FavoriteClassRepository>();
builder.Services.AddScoped<IBookingSnapshotRepository, BookingSnapshotRepository>();
builder.Services.AddScoped<IBookingSnapshotService, BookingSnapshotService>();

builder.Services.AddScoped<IGymService, GymService>();
builder.Services.AddScoped<IBranchService, BranchService>();
builder.Services.AddScoped<ICategoryService, CategoryService>();
builder.Services.AddScoped<IClassService, ClassService>();
builder.Services.AddScoped<IFavoriteGymService, FavoriteGymService>();
builder.Services.AddScoped<IFavoriteClassService, FavoriteClassService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment() || true) // Enable Swagger in all environments for grading/assessment
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "FlexFit Catalog API v1");
    });
}

app.UseHttpsRedirection();
app.UseStaticFiles(); // Serve images from wwwroot/uploads

app.UseAuthentication();
app.UseAuthorization();

// Map REST Controllers
app.MapControllers();

// Map Health Checks
app.MapHealthChecks("/health");

// Map gRPC Service
app.MapGrpcService<CatalogGrpcService>();

app.Run();


