using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.IdentityModel.Tokens;
using SmartShop.Application.Common.Interfaces;
using SmartShop.Application.Interfaces;
using SmartShop.Domain.Interfaces;
using SmartShop.Infrastructure.Caching;
using SmartShop.Infrastructure.Data;
using SmartShop.Infrastructure.Email;
using SmartShop.Infrastructure.Payment;
using SmartShop.Infrastructure.RateLimit;
using SmartShop.Infrastructure.Repositories;
using SmartShop.Infrastructure.Services;
using SmartShop.Infrastructure.UnitOfWork;
using StackExchange.Redis;
using System.Text;

namespace SmartShop.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(this IServiceCollection services, IConfiguration configuration)
    {
        var connectionString = configuration.GetConnectionString("DefaultConnection");

        services.AddDbContext<ApplicationDbContext>((serviceProvider, options) =>
        {
            options.UseSqlServer(connectionString, b => b.MigrationsAssembly(typeof(ApplicationDbContext).Assembly.FullName));
        });

        services.AddScoped<IUnitOfWork, UnitOfWork.UnitOfWork>();
        services.AddScoped<IProductRepository, ProductRepository>();
        services.AddScoped<ICategoryRepository, CategoryRepository>();
        services.AddScoped<IUserRepository, UserRepository>();
        services.AddScoped<ICartRepository, CartRepository>();
        services.AddScoped<IOrderRepository, OrderRepository>();
        services.AddScoped<ICouponRepository, CouponRepository>();
        services.AddScoped<ICouponUsageRepository, CouponUsageRepository>();
        services.AddScoped<IJwtTokenService, JwtTokenService>();
        services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
        services.AddScoped<IProductEmbeddingRepository, ProductEmbeddingRepository>();
        services.AddScoped<IReviewRepository, ReviewRepository>();
        services.AddScoped<IAppSettingRepository, AppSettingRepository>();
        services.AddScoped<IFaqDocumentRepository, FaqDocumentRepository>();
        services.AddScoped<IChatSessionRepository, ChatSessionRepository>();
        services.AddScoped<IChatbotService, ChatbotService>();
        services.AddScoped<IUserAddressRepository, UserAddressRepository>();
        services.AddScoped<IPaymentGateway, VNPayGateway>();
        services.AddScoped<IStoreRepository, StoreRepository>();
        services.AddScoped<IStoreInventoryRepository, StoreInventoryRepository>();

        // AI — chọn provider qua config "AI:Provider" (Groq | Gemini), mặc định Groq
        // Groq: free 500K tokens/day, llama-3.3-70b, nhanh, hiểu tiếng Việt tốt
        // Gemini: free 1M tokens/day, gemini-2.0-flash-lite, hiểu tiếng Việt tốt nhất
        var aiProvider = configuration.GetValue<string>("AI:Provider") ?? "Groq";
        services.AddHttpClient("Groq");
        services.AddHttpClient("Gemini");
        if (aiProvider.Equals("Gemini", StringComparison.OrdinalIgnoreCase))
            services.AddSingleton<ISemanticKernelService, GeminiAIService>();
        else
            services.AddSingleton<ISemanticKernelService, GroqAIService>();
        services.AddHostedService<EmbeddingBackgroundService>();

        // Cache registration with graceful fallback when Redis is unavailable.
        services.AddSingleton<ICacheService>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("CacheRegistration");
            var cacheEnabled = configuration.GetValue("Cache:Enabled", true);
            var cacheProvider = configuration.GetValue<string>("Cache:Provider") ?? "Redis";

            if (!cacheEnabled || cacheProvider.Equals("None", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Cache disabled. Using NoOpCacheService.");
                return new NoOpCacheService();
            }

            if (!cacheProvider.Equals("Redis", StringComparison.OrdinalIgnoreCase))
            {
                logger.LogWarning("Unknown cache provider '{Provider}'. Falling back to NoOpCacheService.", cacheProvider);
                return new NoOpCacheService();
            }

            try
            {
                var redisConnection = configuration.GetConnectionString("Redis") ?? "localhost:6379";
                var redisConfig = ConfigurationOptions.Parse(redisConnection);
                redisConfig.AbortOnConnectFail = false;

                var multiplexer = ConnectionMultiplexer.Connect(redisConfig);
                logger.LogInformation("Cache provider Redis connected successfully.");
                return new RedisCacheService(multiplexer);
            }
            catch (Exception ex)
            {
                logger.LogWarning(ex, "Redis unavailable. Falling back to NoOpCacheService.");
                return new NoOpCacheService();
            }
        });

        // Rate limit store registration — reuses same Redis connection as cache, falls back to in-memory.
        services.AddSingleton<IRateLimitStore>(serviceProvider =>
        {
            var logger = serviceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("RateLimitRegistration");
            var cacheEnabled = configuration.GetValue("Cache:Enabled", true);
            var cacheProvider = configuration.GetValue<string>("Cache:Provider") ?? "Redis";

            if (cacheEnabled && cacheProvider.Equals("Redis", StringComparison.OrdinalIgnoreCase))
            {
                try
                {
                    var redisConnection = configuration.GetConnectionString("Redis") ?? "localhost:6379";
                    var redisConfig = ConfigurationOptions.Parse(redisConnection);
                    redisConfig.AbortOnConnectFail = false;

                    var multiplexer = ConnectionMultiplexer.Connect(redisConfig);
                    var rlLogger = serviceProvider.GetRequiredService<ILoggerFactory>()
                        .CreateLogger<RedisRateLimitStore>();
                    logger.LogInformation("Rate limit store: using Redis.");
                    return new RedisRateLimitStore(multiplexer, rlLogger);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Redis unavailable for rate limiting. Falling back to InMemoryRateLimitStore.");
                }
            }

            logger.LogWarning("Rate limit store: using InMemoryRateLimitStore.");
            return new InMemoryRateLimitStore();
        });

        services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
            .AddJwtBearer(options =>
            {
                options.TokenValidationParameters = new TokenValidationParameters
                {
                    ValidateIssuer = true,
                    ValidateAudience = true,
                    ValidateLifetime = true,
                    ValidateIssuerSigningKey = true,
                    ValidIssuer = configuration["Jwt:Issuer"],
                    ValidAudience = configuration["Jwt:Audience"],
                    IssuerSigningKey = new SymmetricSecurityKey(
                        Encoding.UTF8.GetBytes(configuration["Jwt:Key"]!))
                };
            });

        return services;
    }
}
