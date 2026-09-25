using System.Security.Claims;
using System.Text.Json.Serialization;
using System.Threading.RateLimiting;
using HelpMotivateMe.Api.Services;
using HelpMotivateMe.Core.Interfaces;
using HelpMotivateMe.Core.Options;
using HelpMotivateMe.Infrastructure.Data;
using HelpMotivateMe.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.Data.Sqlite;
using Microsoft.EntityFrameworkCore;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

// Configure forwarded headers only from explicitly configured proxies.
builder.Services.Configure<ForwardedHeadersOptions>(options =>
{
    options.ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                               ForwardedHeaders.XForwardedProto |
                               ForwardedHeaders.XForwardedHost;
});

// Database
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection is required.");
if (connectionString.StartsWith("Data Source", StringComparison.OrdinalIgnoreCase))
{
    var sqliteConnection = new SqliteConnectionStringBuilder(connectionString);
    if (sqliteConnection.DataSource != ":memory:")
    {
        sqliteConnection.DataSource = Path.GetFullPath(sqliteConnection.DataSource);
        Directory.CreateDirectory(Path.GetDirectoryName(sqliteConnection.DataSource)!);
        connectionString = sqliteConnection.ConnectionString;
    }
}

builder.Services.AddSingleton<SqliteConnectionInterceptor>();
builder.Services.AddSingleton<GuidKeyInterceptor>();
builder.Services.AddDbContext<AppDbContext>((services, options) =>
    options.UseSqlite(connectionString)
        .AddInterceptors(
            services.GetRequiredService<SqliteConnectionInterceptor>(),
            services.GetRequiredService<GuidKeyInterceptor>()));

// Data Protection - store keys in database for persistence across restarts and multiple instances
builder.Services.AddDataProtection()
    .SetApplicationName("HelpMotivateMe")
    .PersistKeysToDbContext<AppDbContext>();

// Local File Storage Service
builder.Services.AddSingleton<IStorageService, LocalFileStorageService>();

// OpenAI Service
builder.Services.AddOptions<AiOptions>()
    .Bind(builder.Configuration.GetSection(AiOptions.SectionName))
    .PostConfigure(options =>
    {
        if (string.IsNullOrWhiteSpace(options.ApiKey))
            options.ApiKey = builder.Configuration["OpenAI:ApiKey"] ?? "";
    });
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
builder.Services.AddScoped<IPasswordHasher<HelpMotivateMe.Core.Entities.User>, PasswordHasher<HelpMotivateMe.Core.Entities.User>>();
builder.Services.AddOptions<SingleUserOptions>()
    .Bind(builder.Configuration.GetSection(SingleUserOptions.SectionName))
    .ValidateDataAnnotations()
    .Validate(options => !string.Equals(options.Password, "change-me", StringComparison.OrdinalIgnoreCase),
        "SingleUser:Password must not use the placeholder value 'change-me'.")
    .ValidateOnStart();

// Habit Stack Service
builder.Services.AddScoped<IHabitStackService, HabitStackService>();

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
builder.Services.AddHostedService<SingleUserInitializer>();
builder.Services.AddHostedService<MilestoneSeeder>();

builder.Services.AddRateLimiter(options =>
{
    options.AddFixedWindowLimiter("login", limiter =>
    {
        limiter.PermitLimit = 5;
        limiter.Window = TimeSpan.FromMinutes(1);
        limiter.QueueLimit = 0;
        limiter.AutoReplenishment = true;
    });
});

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
        options.Cookie.SameSite = SameSiteMode.Lax;
        options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;
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
        options.Events.OnValidatePrincipal = async context =>
        {
            var userIdValue = context.Principal?.FindFirstValue(ClaimTypes.NameIdentifier);
            var versionValue = context.Principal?.FindFirstValue("credential_version");
            if (!Guid.TryParse(userIdValue, out var userId) || !int.TryParse(versionValue, out var version))
            {
                context.RejectPrincipal();
                return;
            }

            var db = context.HttpContext.RequestServices.GetRequiredService<AppDbContext>();
            var valid = await db.Users.AnyAsync(user =>
                user.Id == userId && user.IsActive && user.CredentialVersion == version);
            if (!valid) context.RejectPrincipal();
        };
    });

builder.Services.AddControllers()
    .AddJsonOptions(options => { options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()); });
builder.Services.AddOpenApi();

var app = builder.Build();

await using (var scope = app.Services.CreateAsyncScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.EnsureCreatedAsync();
    await SqliteSchemaUpdater.UpgradeAsync(db);
}

app.UseForwardedHeaders();
app.UseDefaultFiles();
app.UseStaticFiles();
if (app.Environment.IsDevelopment()) app.UseCors("AllowFrontend");
app.UseRateLimiter();
app.UseAuthentication();
app.UseSession();
app.UseAuthorization();

app.MapControllers();
app.MapGet("/health/live", () => Results.Ok(new { status = "live" }));
app.MapGet("/health/ready", async (AppDbContext db) =>
    await db.Database.CanConnectAsync()
        ? Results.Ok(new { status = "ready" })
        : Results.StatusCode(StatusCodes.Status503ServiceUnavailable));

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi("/api/openapi/{documentName}.json");
    app.MapScalarApiReference("/api/docs", options => options
        .WithOpenApiRoutePattern("/api/openapi/{documentName}.json")
        .WithTitle("Help Motivate Me - API Reference")
        .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
        .HideDarkModeToggle());
}

app.Map("/api/{**path}", () => Results.NotFound());
app.Map("/health/{**path}", () => Results.NotFound());
app.MapFallbackToFile("index.html");

app.Run();