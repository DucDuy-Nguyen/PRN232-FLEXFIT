using FlexFit.Booking.Repository.Data;
using FlexFit.Booking.Service.ExternalServices.Catalog;
using FlexFit.Booking.API.ExternalServices.Catalog;
using FlexFit.Booking.API.Messaging.Consumers;
using FlexFit.Booking.Service.Messaging.Events;
using FlexFit.Booking.Repository.Repositories;
using FlexFit.Booking.Repository.Repositories.Interfaces;
using FlexFit.Booking.Service.Service;
using FlexFit.Booking.Service.Service.Interfaces;
using FlexFit.Booking.API.BackgroundJobs;
using FlexFit.Caching;
using MassTransit;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using System;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// 1. Connection String and DB Context
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
builder.Services.AddDbContext<BookingDbContext>(options =>
    options.UseSqlServer(connectionString));

// 2. JWT Bearer Configuration
var jwtKey = builder.Configuration["Jwt:Key"] ?? "VeryLongSuperSecureKey1234567890!!";
var keyBytes = Encoding.UTF8.GetBytes(jwtKey);
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(keyBytes),
        ValidateIssuer = true,
        ValidIssuer = builder.Configuration["Jwt:Issuer"] ?? "FlexFitAPI",
        ValidateAudience = true,
        ValidAudience = builder.Configuration["Jwt:Audience"] ?? "FlexFitClient",
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});

// 3. Register Repositories and Services
builder.Services.AddScoped<IBookingRepository, BookingRepository>();
builder.Services.AddScoped<ICheckInRepository, CheckInRepository>();
builder.Services.AddScoped<IBookingService, BookingService>();
builder.Services.AddScoped<ICheckInService, CheckInService>();
builder.Services.AddScoped<IOutboxRepository, OutboxRepository>();
builder.Services.AddScoped<IBookingMaintenanceService, BookingMaintenanceService>();
builder.Services.AddScoped<IBookingPaymentHandler, BookingPaymentHandler>();
builder.Services.AddScoped<IClassScheduleHandler, ClassScheduleHandler>();

// 4. Register REST typed HttpClient & gRPC Client for Catalog Service
var catalogBaseUrl = builder.Configuration["CatalogConfig:BaseUrl"] ?? "http://localhost:7001";
builder.Services.AddHttpClient<ICatalogServiceClient, CatalogServiceClient>(client =>
{
    client.BaseAddress = new Uri(catalogBaseUrl);
});

var catalogGrpcUrl = builder.Configuration["CatalogConfig:GrpcUrl"] ?? "http://localhost:5003";
builder.Services.AddGrpcClient<FlexFit.Catalog.Service.Protos.CatalogGrpc.CatalogGrpcClient>(options =>
{
    options.Address = new Uri(catalogGrpcUrl);
});

// 4.5 Register Redis Caching Shared Library Services
builder.Services.AddFlexFitRedisCaching(builder.Configuration);

// 5. MassTransit with RabbitMQ Setup
builder.Services.AddMassTransit(x =>
{
    // Add Consumers
    x.AddConsumer<CreditDeductionCompletedConsumer>();
    x.AddConsumer<CreditDeductionFailedConsumer>();
    x.AddConsumer<ClassScheduleChangedConsumer>();

    x.UsingRabbitMq((context, cfg) =>
    {
        cfg.Host(builder.Configuration["RabbitMQ:Host"] ?? "localhost", "/", h =>
        {
            h.Username(builder.Configuration["RabbitMQ:Username"] ?? "guest");
            h.Password(builder.Configuration["RabbitMQ:Password"] ?? "guest");
        });

        // Config receive endpoints
        cfg.ReceiveEndpoint("booking-credit-deduction-completed", ep =>
        {
            ep.ConfigureConsumer<CreditDeductionCompletedConsumer>(context);
        });

        cfg.ReceiveEndpoint("booking-credit-deduction-failed", ep =>
        {
            ep.ConfigureConsumer<CreditDeductionFailedConsumer>(context);
        });

        cfg.ReceiveEndpoint("booking-class-schedule-changed", ep =>
        {
            ep.ConfigureConsumer<ClassScheduleChangedConsumer>(context);
        });
    });
});

// 6. Registered Hosted Background Jobs
builder.Services.AddHostedService<BookingReminderJob>();
builder.Services.AddHostedService<AutoCancelExpiredBookingJob>();
builder.Services.AddHostedService<OutboxPublisherJob>();
builder.Services.AddHostedService<PaymentResponseConsumerWorker>();

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// 7. Swagger doc configurations
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FlexFit.BookingService", Version = "v1" });
    
    var securityScheme = new OpenApiSecurityScheme
    {
        Name = "JWT Authentication",
        Description = "Enter your JWT token in the text box below (Bearer [token])",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        Reference = new OpenApiReference
        {
            Id = JwtBearerDefaults.AuthenticationScheme,
            Type = ReferenceType.SecurityScheme
        }
    };
    c.AddSecurityDefinition(securityScheme.Reference.Id, securityScheme);
    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        { securityScheme, Array.Empty<string>() }
    });
});

var app = builder.Build();

// Auto migrate database on startup
using (var scope = app.Services.CreateScope())
{
    try
    {
        var dbContext = scope.ServiceProvider.GetRequiredService<BookingDbContext>();
        dbContext.Database.Migrate();
    }
    catch (Exception ex)
    {
        var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Failed to migrate Booking database.");
    }
}

app.UseSwagger();
app.UseSwaggerUI(c =>
{
    c.SwaggerEndpoint("/swagger/v1/swagger.json", "FlexFit.BookingService v1");
});

app.UseCors(policy => policy
    .AllowAnyOrigin()
    .AllowAnyMethod()
    .AllowAnyHeader());

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
