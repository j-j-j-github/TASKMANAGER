using Microsoft.EntityFrameworkCore;
using TaskManagerApp.Models;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// ==============================================
// 🛠️ SMART DATABASE PATH LOGIC (FIXED)
// ==============================================
string dbPath;

// We check for 'WEBSITE_SITE_NAME', which ONLY exists on Azure App Service.
var azureSiteName = Environment.GetEnvironmentVariable("WEBSITE_SITE_NAME");

if (!string.IsNullOrEmpty(azureSiteName))
{
    // ☁️ AZURE MODE: Save in 'LogFiles' (Writable)
    var azureHome = Environment.GetEnvironmentVariable("HOME");
    dbPath = Path.Combine(azureHome, "LogFiles", "TaskManager_v2.db");
}
else
{
    // 💻 LOCAL MAC/PC MODE: Save in the project folder
    dbPath = Path.Combine(builder.Environment.ContentRootPath, "TaskManager_v2.db");
}

// Update the Connection String dynamically
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite($"Data Source={dbPath}"));

// ==============================================

// Add Authentication Service
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", config =>
    {
        config.LoginPath = "/Account/Login";
        config.AccessDeniedPath = "/Account/Login";
    });

builder.Services.AddControllersWithViews();

var app = builder.Build();

// 🚀 AUTOMATIC MIGRATION
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    context.Database.Migrate(); 
}

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Account}/{action=Register}/{id?}");

app.Run();