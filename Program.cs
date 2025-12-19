using Microsoft.EntityFrameworkCore;
using TaskManagerApp.Models;
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

// Add Database Service
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Add Authentication Service
builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", config =>
    {
        config.LoginPath = "/Account/Login";
        config.AccessDeniedPath = "/Account/Login";
    });

builder.Services.AddControllersWithViews();

var app = builder.Build();

// ==============================================
// 🚀 AUTOMATIC DATABASE MIGRATION
// ==============================================
using (var scope = app.Services.CreateScope())
{
    var services = scope.ServiceProvider;
    var context = services.GetRequiredService<AppDbContext>();
    
    // CHANGE: We switched from EnsureCreated() to Migrate()
    // This allows Azure to update the existing database with your new columns.
    context.Database.Migrate(); 
}
// ==============================================

app.UseStaticFiles();
app.UseRouting();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Tasks}/{action=Index}/{id?}");

app.Run();