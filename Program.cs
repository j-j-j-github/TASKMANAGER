using Microsoft.EntityFrameworkCore;
using TaskManagerApp.Models;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// ==============================================
// 🛠️ SMART DATABASE PATH LOGIC (The Fix)
// ==============================================
string dbPath;

// Check if we are running on Azure (The "HOME" variable is always set on Azure)
var azureHome = Environment.GetEnvironmentVariable("HOME");

if (!string.IsNullOrEmpty(azureHome))
{
    // ☁️ AZURE MODE: Save in the 'LogFiles' folder (Guaranteed to be writable)
    // Works for both Windows (D:\home\LogFiles) and Linux (/home/LogFiles)
    dbPath = Path.Combine(azureHome, "LogFiles", "TaskManager_v2.db");
}
else
{
    // 💻 LOCAL MODE: Save in the project folder
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
    pattern: "{controller=Tasks}/{action=Index}/{id?}");

app.Run();