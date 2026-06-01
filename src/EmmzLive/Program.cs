using EmmzLive.Auth;
using EmmzLive.Configuration;
using EmmzLive.Data;
using EmmzLive.Filters;
using EmmzLive.Infrastructure;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.EntityFrameworkCore;
using Resend;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorPages();

var inboxConfig = new InboxConfig(builder.Configuration["INBOXES"]);
builder.Services.AddSingleton(inboxConfig);
builder.Services.AddScoped<ValidInboxFilter>();

var databaseUrl = builder.Configuration["DATABASE_URL"]
    ?? throw new InvalidOperationException("DATABASE_URL is not configured.");

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(ConnectionStringHelper.ToNpgsqlConnectionString(databaseUrl)));

// Fail fast for required auth configuration.
var sessionSecret = builder.Configuration["SESSION_SECRET"]
    ?? throw new InvalidOperationException("SESSION_SECRET is not configured. Generate one with: openssl rand -base64 32");

var ownerEmail = builder.Configuration["OWNER_EMAIL"]
    ?? throw new InvalidOperationException("OWNER_EMAIL is not configured.");

_ = builder.Configuration["MAIL_FROM"]
    ?? throw new InvalidOperationException(
        "MAIL_FROM is not configured. Set it to a Resend-verified sender address, e.g. 'emmz.live <noreply@emmz.live>'.");

// Magic-link token service — singleton because the key bytes are derived once.
builder.Services.AddSingleton(new MagicLinkTokenService(sessionSecret, ownerEmail));

// Resend email client.
builder.Services.AddResend(options =>
{
    options.ApiToken = builder.Configuration["RESEND_API_KEY"]
        ?? throw new InvalidOperationException("RESEND_API_KEY is not configured.");
});

builder.Services.AddTransient<IMagicLinkEmailSender, ResendMagicLinkEmailSender>();

// Cookie authentication — scheme and cookie both named "anon-inbox-session".
// Session cookie only (IsPersistent = false in SignInAsync); no expiry set here.
builder.Services.AddAuthentication("anon-inbox-session")
    .AddCookie("anon-inbox-session", options =>
    {
        options.Cookie.Name = "anon-inbox-session";
        options.Cookie.HttpOnly = true;
        options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
        options.Cookie.SameSite = SameSiteMode.Strict;
        options.LoginPath = "/auth/request";
    });

builder.Services.AddAuthorization();

var app = builder.Build();

var startupLogger = app.Logger;
startupLogger.LogInformation("Configured inboxes: {Slugs}", string.Join(", ", inboxConfig.Slugs));

// Run EF Core migrations on startup.
using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    await db.Database.MigrateAsync();
}

// Configure the HTTP request pipeline.

// Must be first: honour X-Forwarded-Proto/For from Railway's TLS-terminating proxy.
// KnownNetworks/KnownProxies are cleared so the proxy's forwarded headers are always trusted
// regardless of its IP address (Railway assigns dynamic proxy IPs).
app.UseForwardedHeaders(new ForwardedHeadersOptions
{
    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
    KnownIPNetworks = { },
    KnownProxies = { },
});

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

// Authentication and authorisation after routing, before endpoints.
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
