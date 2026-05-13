using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using SmartShop.Application;
using SmartShop.Application.Common.Interfaces;
using SmartShop.Domain.Interfaces;
using SmartShop.Infrastructure;
using SmartShop.Infrastructure.Data;
using SmartShop.Infrastructure.Email;
using SmartShop.Infrastructure.Repositories;
using SmartShop.WebAPI.Hubs;
using SmartShop.WebAPI.Middleware;
using SmartShop.WebAPI.Options;
using SmartShop.WebAPI.Services;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(opts =>
        opts.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter()));
builder.Services.AddEndpointsApiExplorer();

builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo { Title = "FastFood API", Version = "v1" });
    c.CustomSchemaIds(GetSwaggerSchemaId);

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập JWT token theo định dạng: Bearer {token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference { Type = ReferenceType.SecurityScheme, Id = "Bearer" }
            },
            []
        }
    });
});

builder.Services.Configure<RateLimitOptions>(builder.Configuration.GetSection("RateLimit"));
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration);

// SignalR
builder.Services.AddSignalR();
builder.Services.AddSingleton<Microsoft.AspNetCore.SignalR.IUserIdProvider, SmartShop.WebAPI.Hubs.SubClaimUserIdProvider>();

// HTTP Context for CurrentUserService
builder.Services.AddHttpContextAccessor();

// Sprint 10 services
builder.Services.AddScoped<IEmailService, SmtpEmailService>();
builder.Services.AddScoped<IWishlistRepository, WishlistRepository>();
builder.Services.AddScoped<INotificationRepository, NotificationRepository>();
builder.Services.AddScoped<ICurrentUserService, CurrentUserService>();
builder.Services.AddScoped<INotificationHubService, NotificationHubService>();

builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                builder.Configuration["Cors:AllowedOrigins"]?.Split(',') ?? ["http://localhost:5173"])
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials(); // required for SignalR
    });
});

// JWT SignalR — đọc access_token từ query string cho hub connections
var jwtKey = builder.Configuration["Jwt:Key"]!;
var jwtIssuer = builder.Configuration["Jwt:Issuer"];
var jwtAudience = builder.Configuration["Jwt:Audience"];

builder.Services.PostConfigure<JwtBearerOptions>(JwtBearerDefaults.AuthenticationScheme, options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtIssuer,
        ValidAudience = jwtAudience,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
    };
    options.Events = new JwtBearerEvents
    {
        OnMessageReceived = context =>
        {
            var accessToken = context.Request.Query["access_token"];
            var path = context.HttpContext.Request.Path;
            if (!string.IsNullOrEmpty(accessToken) && path.StartsWithSegments("/hubs"))
                context.Token = accessToken;
            return Task.CompletedTask;
        }
    };
});

var app = builder.Build();

// Auto-migrate on startup in all environments
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();
    await db.Database.MigrateAsync();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.DocumentTitle = "FastFood API Docs";
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "FastFood API v1");
    });

    await DbSeeder.SeedAsync(app.Services);
}

app.UseCors("AllowFrontend");
app.UseStaticFiles(); // serve wwwroot/images/...
app.UseHttpsRedirection();
app.UseMiddleware<ExceptionHandlingMiddleware>();
app.UseMiddleware<RateLimitingMiddleware>();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapHub<OrderStatusHub>("/hubs/orders");

app.Run();

static string GetSwaggerSchemaId(Type type)
{
    if (!type.IsGenericType)
    {
        return (type.FullName ?? type.Name).Replace("+", ".");
    }

    var genericTypeName = type.GetGenericTypeDefinition().FullName
        ?? type.GetGenericTypeDefinition().Name;
    genericTypeName = genericTypeName.Split('`')[0].Replace("+", ".");

    var genericArgumentNames = string.Join("And", type.GetGenericArguments().Select(GetSwaggerSchemaId));
    return $"{genericTypeName}Of{genericArgumentNames}";
}
