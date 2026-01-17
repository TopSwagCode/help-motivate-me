using System.Security.Claims;
using AspNet.Security.OAuth.GitHub;
using AspNet.Security.OAuth.LinkedIn;
using Microsoft.AspNetCore.Authentication.Google;
using HelpMotivateMe.Core.Interfaces;
using HelpMotivateMe.Infrastructure.Data;
using HelpMotivateMe.Api.Services;
using HelpMotivateMe.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Configure forwarded headers for proxy support (Traefik/ngrok)
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor | 
                               ForwardedHeaders.XForwardedProto | 
                               ForwardedHeaders.XForwardedHost;
    options.KnownIPNetworks.Clear();
    options.KnownProxies.Clear();
    
    // Trust all proxies (ngrok/Traefik)
    options.ForwardLimit = null;
});

// Database
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// Data Protection - store keys in database for persistence across restarts and multiple instances
builder.Services.AddDataProtection()
    .SetApplicationName("HelpMotivateMe")
    .PersistKeysToDbContext<AppDbContext>();

// Email Service
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

// Push Notification Service
builder.Services.AddScoped<IPushNotificationService, WebPushNotificationService>();

// Local File Storage Service
builder.Services.AddSingleton<IStorageService, LocalFileStorageService>();

// OpenAI Service
builder.Services.AddHttpClient<IOpenAiService, OpenAiService>();

// Identity Score Service
builder.Services.AddScoped<IdentityScoreService>();

// Database Seeder
builder.Services.AddHostedService<AdminUserSeeder>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        var allowedOrigins = builder.Configuration.GetSection("Cors:AllowedOrigins").Get<string[]>()
            ?? ["http://localhost:5173"];

        policy.WithOrigins(allowedOrigins)
              .AllowCredentials()
              .AllowAnyHeader()
              .AllowAnyMethod();
    });
});

// Authentication
builder.Services.AddAuthentication(options =>
    {
        options.DefaultScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(options =>
    {
        options.Cookie.Name = ".HelpMotivateMe.Auth";
        options.Cookie.HttpOnly = true;
        options.Cookie.SameSite = builder.Environment.IsDevelopment()
            ? SameSiteMode.Lax
            : SameSiteMode.None;
        options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
            ? CookieSecurePolicy.SameAsRequest
            : CookieSecurePolicy.Always;
        options.ExpireTimeSpan = TimeSpan.FromDays(30);
        options.SlidingExpiration = true;

        // API responses instead of redirects
        options.Events.OnRedirectToLogin = context =>
        {
            context.Response.StatusCode = 401;
            return Task.CompletedTask;
        };
        options.Events.OnRedirectToAccessDenied = context =>
        {
            context.Response.StatusCode = 403;
            return Task.CompletedTask;
        };
    })
    .AddGitHub(options =>
    {
        options.ClientId = builder.Configuration["OAuth:GitHub:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["OAuth:GitHub:ClientSecret"] ?? "";
        options.Scope.Add("user:email");
        options.SaveTokens = true;
        options.CallbackPath = "/api/signin-github";

        options.Events.OnCreatingTicket = context =>
        {
            if (context.Principal?.Identity is ClaimsIdentity identity)
            {
                identity.AddClaim(new Claim("urn:github:login", context.User.GetProperty("login").GetString() ?? ""));
                identity.AddClaim(new Claim("urn:github:avatar",
                    context.User.GetProperty("avatar_url").GetString() ?? ""));
            }

            return Task.CompletedTask;
        };
    })
    .AddGoogle(options =>
    {
        options.ClientId = builder.Configuration["OAuth:Google:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["OAuth:Google:ClientSecret"] ?? "";
        options.SaveTokens = true;
        options.CallbackPath = "/api/signin-google";
    })
    .AddLinkedIn(options =>
    {
        options.ClientId = builder.Configuration["OAuth:LinkedIn:ClientId"] ?? "";
        options.ClientSecret = builder.Configuration["OAuth:LinkedIn:ClientSecret"] ?? "";
        options.SaveTokens = true;
        options.CallbackPath = "/api/signin-linkedin";
    });

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new System.Text.Json.Serialization.JsonStringEnumConverter());
    });
builder.Services.AddOpenApi();

var app = builder.Build();

// Use forwarded headers from Traefik/ngrok
app.UseForwardedHeaders();

// Force HTTPS scheme when behind proxy (ngrok)
app.Use(async (context, next) =>
{
    // If not localhost and not already HTTPS, force HTTPS scheme
    if (!context.Request.Host.Host.Contains("localhost") && 
        context.Request.Scheme != "https")
    {
        context.Request.Scheme = "https";
    }
    await next();
});

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

// Don't use HTTPS redirection when behind a proxy (Traefik/ngrok handles HTTPS)
// app.UseHttpsRedirection();

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Apply migrations on startup with lock to prevent race conditions
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    var logger = scope.ServiceProvider.GetRequiredService<ILogger<Program>>();

    // Use PostgreSQL advisory lock to ensure only one instance runs migrations
    var connection = db.Database.GetDbConnection();
    await connection.OpenAsync();

    try
    {
        // Try to acquire advisory lock (lock id: 1)
        await using var lockCommand = connection.CreateCommand();
        lockCommand.CommandText = "SELECT pg_try_advisory_lock(1)";
        var acquired = (bool)(await lockCommand.ExecuteScalarAsync())!;

        if (acquired)
        {
            logger.LogInformation("Acquired migration lock, applying migrations...");
            db.Database.Migrate();
            logger.LogInformation("Migrations applied successfully");

            // Release the lock
            await using var unlockCommand = connection.CreateCommand();
            unlockCommand.CommandText = "SELECT pg_advisory_unlock(1)";
            await unlockCommand.ExecuteScalarAsync();
        }
        else
        {
            logger.LogInformation("Another instance is running migrations, waiting...");
            // Wait for migrations to complete by trying to get a blocking lock
            await using var waitCommand = connection.CreateCommand();
            waitCommand.CommandText = "SELECT pg_advisory_lock(1)";
            await waitCommand.ExecuteScalarAsync();

            // Release immediately - we just wanted to wait
            await using var unlockCommand = connection.CreateCommand();
            unlockCommand.CommandText = "SELECT pg_advisory_unlock(1)";
            await unlockCommand.ExecuteScalarAsync();
            logger.LogInformation("Migrations completed by another instance");
        }
    }
    finally
    {
        await connection.CloseAsync();
    }
}

app.Run();
