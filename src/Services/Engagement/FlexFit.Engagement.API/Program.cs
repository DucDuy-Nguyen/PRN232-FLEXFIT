using FlexFit.Engagement.API.Data;
using FlexFit.Engagement.API.Data.Repositories.Interfaces;
using FlexFit.Engagement.API.Data.Repositories.Implementations;
using FlexFit.Engagement.API.Services.Interfaces;
using FlexFit.Engagement.API.Services.Implementations;
using FlexFit.Engagement.API.Services.AI;
using FlexFit.Engagement.API.Redis;
using FlexFit.Engagement.API.Hubs;
using FlexFit.Engagement.API.BackgroundServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using StackExchange.Redis;
using System.Text;
using System.Threading.Tasks;

var builder = WebApplication.CreateBuilder(args);

// ==============================
// 1️⃣ Add Services
// ==============================

// Add DbContext (SQL Server)
builder.Services.AddDbContext<EngagementDbContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Controllers
builder.Services.AddControllers();

// Add HttpContextAccessor for SystemLog ip and user resolution
builder.Services.AddHttpContextAccessor();

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
    // Allow JWT access token to be passed in query string for SignalR
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"].ToString();
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs/notifications"))
            {
                context.Token = accessToken;
            }
            return Task.CompletedTask;
        }
    };
});

builder.Services.AddAuthorization();

// ==============================
// Redis
// ==============================
builder.Services.AddSingleton<IConnectionMultiplexer>(_ =>
{
    var connectionString = builder.Configuration["Redis:ConnectionString"]
        ?? throw new InvalidOperationException("Redis connection string is missing.");

    return ConnectionMultiplexer.Connect(connectionString);
});
builder.Services.AddSingleton<RedisPublisher>();
builder.Services.AddSingleton<RedisSubscriber>();

// ==============================
// DI Repositories & Services
// ==============================

// Notification
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<IEngagementUserRepository, EngagementUserRepository>();
builder.Services.AddScoped<INotificationService, NotificationService>();

// Review
builder.Services.AddScoped<IReviewRepository, ReviewRepository>();
builder.Services.AddScoped<IReviewService, ReviewService>();

// Promotion
builder.Services.AddScoped<IPromotionRepository, PromotionRepository>();
builder.Services.AddScoped<IPromotionService, PromotionService>();

// WorkoutHistory
builder.Services.AddScoped<IWorkoutHistoryRepository, WorkoutHistoryRepository>();
builder.Services.AddScoped<IWorkoutHistoryService, WorkoutHistoryService>();

// SystemLog
builder.Services.AddScoped<ISystemLogRepository, SystemLogRepository>();
builder.Services.AddScoped<ISystemLogService, SystemLogService>();

// AI
builder.Services.AddScoped<IAIContextBuilder, AIContextBuilder>();
builder.Services.AddHttpClient<IAIService, AIService>();

// gRPC Clients
builder.Services.AddGrpcClient<FlexFit.Recommendation.Grpc.RecommendationService.RecommendationServiceClient>(o =>
{
    o.Address = new Uri(builder.Configuration["GrpcSettings:RecommendationUrl"] ?? "http://localhost:5001");
});

// SignalR
builder.Services.AddSignalR();

// Background Services
builder.Services.AddHostedService<RedisSubscriberBackgroundService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
    {
        policy.AllowAnyOrigin()
              .AllowAnyMethod()
              .AllowAnyHeader();
    });
});

var app = builder.Build();

// ==============================
// 2️⃣ Configure Middleware
// ==============================

app.UseSwagger();
app.UseSwaggerUI();

app.UseCors("AllowAll");

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Map SignalR Hub
app.MapHub<NotificationHub>("/hubs/notifications");

app.Run();
