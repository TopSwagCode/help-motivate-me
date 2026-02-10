using System.Security.Claims;
using System.Text.Json.Serialization;
using HelpMotivateMe.Api.Services;
using HelpMotivateMe.Core.Interfaces;
using HelpMotivateMe.Core.Localization.EmailTemplates;
using HelpMotivateMe.Core.Options;
using HelpMotivateMe.Infrastructure.Data;
using HelpMotivateMe.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using MinimalWorker;
using Scalar.AspNetCore;

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
builder.Services.AddScoped<ScheduledPushNotificationService>();

// Local File Storage Service
builder.Services.AddSingleton<IStorageService, LocalFileStorageService>();

// OpenAI Service
builder.Services.AddHttpClient<IOpenAiService, OpenAiService>();

// AI Budget Service
builder.Services.Configure<AiBudgetOptions>(builder.Configuration.GetSection(AiBudgetOptions.SectionName));
builder.Services.AddScoped<IAiBudgetService, AiBudgetService>();

// Identity Score Service
builder.Services.AddScoped<IIdentityScoreService, IdentityScoreService>();

// Today View Service
builder.Services.AddScoped<ITodayViewService, TodayViewService>();

// Daily Commitment Service
builder.Services.AddScoped<IDailyCommitmentService, DailyCommitmentService>();

// Identity Proof Service
builder.Services.AddScoped<IIdentityProofService, IdentityProofService>();

// Auth Service
builder.Services.AddScoped<IAuthService, AuthService>();

// Accountability Buddy Service
builder.Services.AddScoped<IAccountabilityBuddyService, AccountabilityBuddyService>();

// Habit Stack Service
builder.Services.AddScoped<IHabitStackService, HabitStackService>();

// Admin Service
builder.Services.AddScoped<IAdminService, AdminService>();

// Daily Commitment Notification Service
builder.Services.AddScoped<IDailyCommitmentNotificationService, DailyCommitmentNotificationService>();

// Analytics Service
builder.Services.AddScoped<IAnalyticsService, AnalyticsService>();

// Milestone Service
builder.Services.AddScoped<IMilestoneService, MilestoneService>();

// Query Interface - read-only queries with AsNoTracking for better performance
builder.Services.AddScoped(typeof(IQueryInterface<>), typeof(QueryInterface<>));

// Resource Authorization Service
builder.Services.AddHttpContextAccessor();
builder.Services.AddScoped<IResourceAuthorizationService, ResourceAuthorizationService>();

// Session (for analytics session tracking)
builder.Services.AddDistributedMemoryCache();
builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.Name = ".HelpMotivateMe.Session";
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
    options.Cookie.SameSite = builder.Environment.IsDevelopment()
        ? SameSiteMode.Lax
        : SameSiteMode.None;
    options.Cookie.SecurePolicy = builder.Environment.IsDevelopment()
        ? CookieSecurePolicy.SameAsRequest
        : CookieSecurePolicy.Always;
});

// Database Seeders
builder.Services.AddHostedService<AdminUserSeeder>();
builder.Services.AddHostedService<MilestoneSeeder>();

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
    })
    .AddFacebook(options =>
    {
        options.AppId = builder.Configuration["OAuth:Facebook:AppId"] ?? "";
        options.AppSecret = builder.Configuration["OAuth:Facebook:AppSecret"] ?? "";
        options.SaveTokens = true;
        options.CallbackPath = "/api/signin-facebook";
    });

builder.Services.AddControllers()
    .AddJsonOptions(options => { options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); });
builder.Services.AddOpenApi();

var app = builder.Build();

// Configure email templates to use hosted images (better email client compatibility)
EmailTemplateBase.FrontendUrl = builder.Configuration["FrontendUrl"];

// Use forwarded headers from Traefik/ngrok
app.UseForwardedHeaders();

// Force HTTPS scheme when behind proxy (ngrok)
app.Use(async (context, next) =>
{
    // If not localhost and not already HTTPS, force HTTPS scheme
    if (!context.Request.Host.Host.Contains("localhost") &&
        context.Request.Scheme != "https")
        context.Request.Scheme = "https";
    await next();
});

// Configure the HTTP request pipeline

// Don't use HTTPS redirection when behind a proxy (Traefik/ngrok handles HTTPS)
// app.UseHttpsRedirection();

app.UseCors("AllowFrontend");
app.UseAuthentication();
app.UseSession();
app.UseAuthorization();

app.MapControllers();

// OpenAPI + Scalar API Docs (admin-only)
app.MapOpenApi("/api/openapi/{documentName}.json")
    .RequireAuthorization(policy => policy.RequireRole("Admin"));

var frontendUrl = builder.Configuration["FrontendUrl"] ?? "http://localhost:5173";
app.MapScalarApiReference("/api/docs", options =>
{
    options
        .WithOpenApiRoutePattern("/api/openapi/{documentName}.json")
        .WithTitle("Help Motivate Me - API Reference")
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
        .HideDarkModeToggle()
        .AddHeadContent($@"
            <link rel=""preconnect"" href=""https://fonts.googleapis.com"" />
            <link rel=""preconnect"" href=""https://fonts.gstatic.com"" crossorigin />
            <link href=""https://fonts.googleapis.com/css2?family=Nunito:wght@400;500;600;700&family=Inter:wght@400;500;600;700&display=swap"" rel=""stylesheet"" />
            <style>
                :root {{
                    --scalar-background-1: #faf7f2;
                    --scalar-background-2: #f5f0e8;
                    --scalar-background-3: #fdfcfa;
                    --scalar-color-1: #5f483d;
                    --scalar-color-2: #8a6a54;
                    --scalar-color-3: #9a7d64;
                    --scalar-color-accent: #d4944c;
                    --scalar-border-color: rgba(212, 148, 76, 0.2);
                    --scalar-sidebar-background-1: #f5f0e8;
                    --scalar-sidebar-color-1: #5f483d;
                    --scalar-sidebar-color-2: #8a6a54;
                    --scalar-button-1: #d4944c;
                    --scalar-button-1-hover: #b87a3a;
                    --scalar-button-1-color: #ffffff;
                    --scalar-font: 'Nunito', 'Inter', system-ui, sans-serif;
                }}
                .back-to-admin {{
                    position: fixed;
                    top: 12px;
                    right: 16px;
                    z-index: 1000;
                    display: inline-flex;
                    align-items: center;
                    gap: 6px;
                    padding: 6px 14px;
                    font-family: 'Nunito', sans-serif;
                    font-size: 13px;
                    font-weight: 600;
                    color: #8a6a54;
                    background: #f5f0e8;
                    border: 1px solid rgba(212, 148, 76, 0.3);
                    border-radius: 999px;
                    text-decoration: none;
                    transition: background 0.15s, color 0.15s;
                }}
                .back-to-admin:hover {{
                    background: #d4944c;
                    color: #fff;
                }}
            </style>
        ")
        .AddHeaderContent($@"
            <a class=""back-to-admin"" href=""{frontendUrl}/admin"">
                <svg xmlns=""http://www.w3.org/2000/svg"" width=""14"" height=""14"" viewBox=""0 0 20 20"" fill=""currentColor"">
                    <path fill-rule=""evenodd"" d=""M9.707 16.707a1 1 0 01-1.414 0l-6-6a1 1 0 010-1.414l6-6a1 1 0 011.414 1.414L5.414 9H17a1 1 0 110 2H5.414l4.293 4.293a1 1 0 010 1.414z"" clip-rule=""evenodd"" />
                </svg>
                Admin Dashboard
            </a>
        ");
})
.RequireAuthorization(policy => policy.RequireRole("Admin"));

// Background Workers - Scheduled Push Notifications
// app.RunCronBackgroundWorker("0 */6 * * *",
//     async (CancellationToken ct, ScheduledPushNotificationService service, ILogger<Program> logger) =>
// {
//     logger.LogInformation("Scheduled push notification worker executing at {Time}", DateTime.UtcNow);
//     await service.SendScheduledNotificationAsync();
// })
// .WithName("scheduled-push-notifications")
// .WithErrorHandler(ex =>
// {
//     var logger = app.Services.GetRequiredService<ILogger<Program>>();
//     logger.LogError(ex, "Error in scheduled push notification worker");
// });

app.RunPeriodicBackgroundWorker(TimeSpan.FromMinutes(5), async (ILogger<Program> logger) =>
{
    logger.LogInformation("Heartbeat at {Time}", DateTime.UtcNow);
    await Task.Delay(1);
});

// Background Worker - Daily Identity Commitment Notifications
app.RunPeriodicBackgroundWorker(TimeSpan.FromMinutes(5),
        async (CancellationToken ct, IDailyCommitmentNotificationService service, ILogger<Program> logger) =>
        {
            logger.LogInformation("Daily commitment notification worker executing at {Time}", DateTime.UtcNow);
            var sentCount = await service.ProcessEligibleUsersAsync(ct);
            logger.LogInformation("Daily commitment notification worker completed. Sent {SentCount} notifications",
                sentCount);
        })
    .WithName("daily-commitment-notifications")
    .WithErrorHandler(ex =>
    {
        var logger = app.Services.GetRequiredService<ILogger<Program>>();
        logger.LogError(ex, "Error in daily commitment notification worker");
    });

app.Run();