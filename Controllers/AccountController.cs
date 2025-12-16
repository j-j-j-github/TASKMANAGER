using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using System.Security.Claims; // Needed for creating the User Identity
using TaskManagerApp.Models;  // Needed to access User and DbContext

namespace TaskManagerApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        // Constructor: Receives the Database Tool we set up in Program.cs
        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // GET: Shows the login page
        public IActionResult Login()
        {
            // If already logged in, go to Dashboard
            if (User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Tasks");
            }
            return View();
        }

        // GET: /Account/Register
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 1. Check if email already exists
                if (_context.Users.Any(u => u.Email == model.Email))
                {
                    ModelState.AddModelError("", "Email is already taken.");
                    return View(model);
                }

                // 2. Create the new User
                var newUser = new User
                {
                    FullName = model.FullName,
                    Email = model.Email,
                    PasswordHash = model.Password, // In a real app, we would encrypt this!
                    Role = "User" // Default role is "User", not "Admin"
                };

                _context.Users.Add(newUser);
                await _context.SaveChangesAsync();

                // 3. Go to Login Page
                return RedirectToAction("Login");
            }

            return View(model);
        }

        // POST: Handles the submit button
        [HttpPost]
        public async Task<IActionResult> Login(LoginViewModel model)
        {
            if (ModelState.IsValid)
            {
                // 1. Check Database for User
                var user = _context.Users.FirstOrDefault(u => u.Email == model.Email && u.PasswordHash == model.Password);

                if (user != null)
                {
                    // 2. Create the User's ID Badge (Claims)
                    var claims = new List<Claim>
                    {
                        new Claim(ClaimTypes.Name, user.Email),
                        new Claim(ClaimTypes.NameIdentifier, user.UserID.ToString()),
                        new Claim(ClaimTypes.Role, user.Role)
                    };

                    // 3. Create Identity & Sign In
                    var claimsIdentity = new ClaimsIdentity(claims, "CookieAuth");
                    await HttpContext.SignInAsync("CookieAuth", new ClaimsPrincipal(claimsIdentity));

                    return RedirectToAction("Index", "Tasks");
                }

                // If login fails
                ModelState.AddModelError("", "Invalid Email or Password");
            }

            return View(model);
        }

        // LOGOUT
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            return RedirectToAction("Login");
        }
    }
}