using Microsoft.EntityFrameworkCore;
using TaskManagerApp.Models; 
using Microsoft.AspNetCore.Authentication.Cookies;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddAuthentication("CookieAuth")
    .AddCookie("CookieAuth", config =>
    {
        config.LoginPath = "/Account/Login"; 
        config.AccessDeniedPath = "/Account/Login";
    });

builder.Services.AddControllersWithViews();

var app = builder.Build();

using (var scope = app.Services.CreateScope())
{
    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    
    context.Database.EnsureCreated();

    if (!context.Users.Any())
    {
        context.Users.Add(new User { FullName = "System Admin", Email = "admin@domain.com", PasswordHash = "123", Role = "Admin" });
        context.Users.Add(new User { FullName = "John Doe", Email = "john@domain.com", PasswordHash = "123", Role = "User" });
        context.SaveChanges();
    }
}

app.UseStaticFiles(); 
app.UseRouting();

app.UseAuthentication(); 
app.UseAuthorization();  

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Tasks}/{action=Index}/{id?}");

app.Run();