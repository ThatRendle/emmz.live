using EmmzLive.Configuration;
using EmmzLive.Data;
using EmmzLive.Filters;
using EmmzLive.Infrastructure;
using Microsoft.EntityFrameworkCore;

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
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();

app.UseRouting();

app.UseAuthorization();

app.MapStaticAssets();
app.MapRazorPages()
   .WithStaticAssets();

app.Run();
