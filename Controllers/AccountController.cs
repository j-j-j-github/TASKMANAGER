using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using TaskManagerApp.Models;

namespace TaskManagerApp.Controllers
{
    public class AccountController : Controller
    {
        private readonly AppDbContext _context;

        public AccountController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Account/Register
        [HttpGet]
        public IActionResult Register()
        {
            return View();
        }

        // POST: /Account/Register
        [HttpPost]
        public async Task<IActionResult> Register(string FullName, string Email, string Password, string UserRole, string ProjectName, string InviteCode)
        {
            // 1. Basic Validation
            if (string.IsNullOrWhiteSpace(FullName) || string.IsNullOrWhiteSpace(Email) || string.IsNullOrWhiteSpace(Password))
            {
                ViewData["RegisterError"] = "All fields are required.";
                return View();
            }

            // 2. Check if user already exists
            if (_context.Users.Any(u => u.Email == Email))
            {
                ViewData["RegisterError"] = "Email is already in use.";
                return View();
            }

            int assignedProjectId = 0;
            string assignedRole = "User";

            // 3. The Fork in the Road (Create Team vs Join Team)
            if (UserRole == "Admin")
            {
                // --- PATH A: CREATE NEW TEAM ---
                if (string.IsNullOrWhiteSpace(ProjectName))
                {
                    ViewData["RegisterError"] = "Team Name is required to create a team.";
                    return View();
                }

                var newProject = new Project
                {
                    Name = ProjectName,
                    // Generate a simple 6-char code
                    InviteCode = Guid.NewGuid().ToString().Substring(0, 6).ToUpper()
                };

                _context.Projects.Add(newProject);
                await _context.SaveChangesAsync();

                assignedProjectId = newProject.Id;
                assignedRole = "Admin";
            }
            else
            {
                // --- PATH B: JOIN EXISTING TEAM ---
                if (string.IsNullOrWhiteSpace(InviteCode))
                {
                    ViewData["RegisterError"] = "Invite Code is required to join a team.";
                    return View();
                }

                var project = _context.Projects.FirstOrDefault(p => p.InviteCode == InviteCode);
                if (project == null)
                {
                    ViewData["RegisterError"] = "Invalid Invite Code. Please check with your admin.";
                    return View();
                }

                assignedProjectId = project.Id;
                assignedRole = "User";
            }

            // 4. Create the User (WITH HASHING)
            
            // 🔒 SECURITY UPGRADE: Hash the password before saving
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(Password);

            var user = new User
            {
                FullName = FullName,
                Email = Email,
                PasswordHash = passwordHash, // Save the HASH, not the plain text
                Role = assignedRole,
                ProjectId = assignedProjectId
            };

            _context.Users.Add(user);
            await _context.SaveChangesAsync();

            // 5. SUCCESS! 
            ViewBag.RegisterSuccess = true;
            return View();
        }

        // GET: /Account/Login
        [HttpGet]
        public IActionResult Login()
        {
            // If already logged in, go to Dashboard
            if (User.Identity!.IsAuthenticated)
            {
                return RedirectToAction("Index", "Tasks");
            }
            return View();
        }

        // POST: /Account/Login
        [HttpPost]
        public async Task<IActionResult> Login(string email, string password)
        {
            if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(password))
            {
                ViewData["LoginError"] = "Please enter both email and password.";
                return View();
            }

            // 🔒 SECURITY UPGRADE: Verify Hash
            
            // 1. Get user by Email ONLY
            var user = _context.Users.FirstOrDefault(u => u.Email == email);

            // 2. If user exists, check password using BCrypt.Verify
            if (user != null)
            {
                bool isPasswordValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

                if (isPasswordValid)
                {
                    // 3. Log them in
                    await SignInUser(user);
                    return RedirectToAction("Index", "Tasks");
                }
            }

            // 4. Login Failed
            ViewData["LoginError"] = "Invalid Email or Password.";
            return View();
        }

        // GET: /Account/Logout
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync("CookieAuth");
            return RedirectToAction("Login");
        }

        // --- HELPER: LOG USER IN ---
        private async Task SignInUser(User user)
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), 
                new Claim(ClaimTypes.Name, user.Email),
                new Claim(ClaimTypes.Role, user.Role),
                new Claim("ProjectId", user.ProjectId.ToString()) 
            };

            var identity = new ClaimsIdentity(claims, "CookieAuth");
            var principal = new ClaimsPrincipal(identity);

            await HttpContext.SignInAsync("CookieAuth", principal);
        }
    }
}