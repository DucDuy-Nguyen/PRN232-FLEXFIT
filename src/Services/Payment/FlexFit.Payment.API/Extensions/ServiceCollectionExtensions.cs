using System;
using System.Text;
using FlexFit.Payment.Service.Configurations;
using FlexFit.Payment.Service.Interfaces;
using FlexFit.Payment.Service.Services;
using FlexFit.Payment.Repository.Interfaces;
using FlexFit.Payment.Repository.Repositories;
using FlexFit.Payment.Repository.Data;
using FlexFit.Payment.API.Gateways.PayOS;
using FlexFit.Payment.API.Infrastructure.Redis;
using FlexFit.Payment.API.BackgroundServices;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PayOS;
using StackExchange.Redis;

namespace FlexFit.Payment.API.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddPaymentApplication(this IServiceCollection services)
        {
            services.AddScoped<IPaymentService, PaymentService>();
            services.AddScoped<ICreditService, CreditService>();
            services.AddScoped<ICreditAdjustmentService, CreditAdjustmentService>();

            // Hosted Services (Background workers)
            services.AddHostedService<OutboxPublisherWorker>();
            services.AddHostedService<RedisConsumerWorker>();
            services.AddHostedService<PendingMessageRecoveryWorker>();

            return services;
        }

        public static IServiceCollection AddPaymentInfrastructure(this IServiceCollection services, IConfiguration configuration)
        {
            // Bind Options
            services.AddOptions<FlexFit.Payment.Service.Configurations.PayOSOptions>()
                .Bind(configuration.GetSection("PayOS"));

            services.AddOptions<PaymentOptions>()
                .Bind(configuration.GetSection("Payment"));

            // Fail fast check on configuration
            var payOsSection = configuration.GetSection("PayOS");
            var paymentSection = configuration.GetSection("Payment");
            var useMock = paymentSection.GetValue<bool>("UseMockPayment");

            if (!useMock)
            {
                var clientId = payOsSection["ClientId"];
                var apiKey = payOsSection["ApiKey"];
                var checksumKey = payOsSection["ChecksumKey"];

                if (string.IsNullOrWhiteSpace(clientId) ||
                    string.IsNullOrWhiteSpace(apiKey) ||
                    string.IsNullOrWhiteSpace(checksumKey))
                {
                    throw new InvalidOperationException("Cấu hình PayOS bị thiếu hoặc không hợp lệ khi UseMockPayment là false.");
                }
            }

            // DbContext (SQL Server)
            services.AddDbContext<PaymentDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // Redis connection multiplexer
            services.AddSingleton<IConnectionMultiplexer>(sp =>
            {
                var connString = configuration["Redis:ConnectionString"] ?? "localhost:6379,abortConnect=false";
                var options = ConfigurationOptions.Parse(connString);
                options.AbortOnConnectFail = false;
                return ConnectionMultiplexer.Connect(options);
            });

            // PayOS Client
            services.AddSingleton(sp =>
            {
                var options = sp.GetRequiredService<IOptions<FlexFit.Payment.Service.Configurations.PayOSOptions>>().Value;
                return new PayOSClient(options.ClientId, options.ApiKey, options.ChecksumKey);
            });

            // Repositories
            services.AddScoped<IPaymentRepository, PaymentRepository>();
            services.AddScoped<ICreditRepository, CreditRepository>();
            services.AddScoped<IOutboxRepository, OutboxRepository>();
            services.AddScoped<IProcessedMessageRepository, ProcessedMessageRepository>();

            // Infrastructure services
            services.AddScoped<IDistributedLockService, RedisDistributedLockService>();
            services.AddScoped<ICacheService, RedisCacheService>();
            services.AddScoped<IIdempotencyService, RedisIdempotencyService>();
            services.AddScoped<IEventPublisher, RedisEventPublisher>();
            services.AddScoped<IPayOSPaymentGateway, PayOSPaymentGateway>();

            return services;
        }

        public static IServiceCollection AddPaymentAuthentication(this IServiceCollection services, IConfiguration configuration)
        {
            services.AddAuthentication(options =>
            {
                options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
            })
            .AddJwtBearer(options =>
            {
                var key = Encoding.UTF8.GetBytes(configuration["Jwt:Key"] ?? "VeryLongSuperSecureKey1234567890!!");
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"] ?? "FlexFitAPI",
                    ValidAudience = configuration["Jwt:Audience"] ?? "FlexFitClient",
                    IssuerSigningKey = new SymmetricSecurityKey(key)
                };
            });

            services.AddAuthorization();
            services.AddHttpContextAccessor();

            return services;
        }

        public static IServiceCollection AddPaymentSwagger(this IServiceCollection services)
        {
            services.AddEndpointsApiExplorer();
            services.AddSwaggerGen(c =>
            {
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

            return services;
        }
    }
}
