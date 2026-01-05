using System.Security.Claims;
using AspNet.Security.OAuth.GitHub;
using HelpMotivateMe.Core.Interfaces;
using HelpMotivateMe.Infrastructure.Data;
using HelpMotivateMe.Infrastructure.Services;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
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

// Email Service
builder.Services.AddScoped<IEmailService, SmtpEmailService>();

// Local File Storage Service
builder.Services.AddSingleton<IStorageService, LocalFileStorageService>();

// OpenAI Service
builder.Services.AddHttpClient<IOpenAiService, OpenAiService>();

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

// Apply migrations on startup in development
if (app.Environment.IsDevelopment())
{
    using var scope = app.Services.CreateScope();
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.Migrate();
}

app.Run();
