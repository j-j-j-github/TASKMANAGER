using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication; // Needed for SignOut
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using TaskManagerApp.Models;

namespace TaskManagerApp.Controllers
{
    [Authorize] // 🔒 Locks the controller. Only logged-in users enter.
    public class TasksController : Controller
    {
        private readonly AppDbContext _context;

        public TasksController(AppDbContext context)
        {
            _context = context;
        }

        // --- 1. DASHBOARD VIEW ---
        public async Task<IActionResult> Index()
        {
            // 1. Get Current User Info
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (userIdClaim != null)
            {
                ViewBag.CurrentUserId = int.Parse(userIdClaim.Value);
            }
            
            ViewBag.UserRole = User.FindFirst(ClaimTypes.Role)?.Value;

            // 2. Get Project ID
            int projectId = GetProjectId();

            // 3. FETCH PROJECT DETAILS
            var project = await _context.Projects.FindAsync(projectId);
            if (project != null)
            {
                ViewBag.TeamName = project.Name;
                ViewBag.InviteCode = project.InviteCode;
            }

            // 4. FIND THE TEAM ADMIN'S NAME
            var adminUser = await _context.Users
                .FirstOrDefaultAsync(u => u.ProjectId == projectId && u.Role == "Admin");

            ViewBag.AdminName = adminUser?.FullName ?? "System"; 

            // 5. Get Team Members (For the Dropdown)
            ViewBag.UsersList = _context.Users
                .Where(u => u.ProjectId == projectId)
                .ToList();

            return View();
        }

        // --- 2. AJAX API: GET TASKS (Search & Filter) ---
        [HttpGet]
        public async Task<IActionResult> GetTasks(string? term)
        {
            int projectId = GetProjectId();
            
            var roleClaim = User.FindFirst(ClaimTypes.Role);
            bool isAdmin = (roleClaim != null) && roleClaim.Value == "Admin";

            var query = _context.Tasks
                .Where(t => t.ProjectId == projectId)
                .Include(t => t.AssignedUser) 
                .AsQueryable();

            if (!string.IsNullOrEmpty(term))
            {
                string lowerTerm = term.ToLower();
                query = query.Where(t => 
                    t.Title.ToLower().Contains(lowerTerm) || 
                    (t.Description != null && t.Description.ToLower().Contains(lowerTerm)) ||
                    (t.AssignedUser != null && t.AssignedUser.FullName.ToLower().Contains(lowerTerm))
                );
            }

            var tasks = await query.Select(t => new {
                t.Id, 
                t.Title,
                t.Description,
                t.Priority,
                t.Status,
                Deadline = t.DueDate.ToString("yyyy-MM-dd"), 
                AssignedTo = t.AssignedTo,
                AssignedToName = t.AssignedUser != null ? t.AssignedUser.FullName : "Unassigned",
                AssignedToEmail = t.AssignedUser != null ? t.AssignedUser.Email : "",
                CanManage = isAdmin 
            }).ToListAsync();

            return Json(tasks);
        }

        // --- 3. AJAX API: SAVE TASK (Create or Edit) ---
        [HttpPost]
        public async Task<IActionResult> SaveTask([FromBody] TaskItem task)
        {
            int projectId = GetProjectId();

            if (task.DueDate == DateTime.MinValue)
            {
                task.DueDate = DateTime.Now.AddDays(7);
            }

            if (task.Id == 0)
            {
                task.ProjectId = projectId; 
                _context.Tasks.Add(task);
            }
            else
            {
                var existingTask = await _context.Tasks
                    .FirstOrDefaultAsync(t => t.Id == task.Id && t.ProjectId == projectId);
                
                if (existingTask == null) return NotFound(); 

                existingTask.Title = task.Title;
                existingTask.Description = task.Description;
                existingTask.Status = task.Status;
                existingTask.Priority = task.Priority;
                existingTask.AssignedTo = task.AssignedTo;
                
                if (task.DueDate != DateTime.MinValue)
                {
                    existingTask.DueDate = task.DueDate;
                }
            }

            await _context.SaveChangesAsync();
            return Ok();
        }

        // --- 4. AJAX API: DELETE TASK ---
        [HttpPost]
        public async Task<IActionResult> DeleteTask(int id)
        {
            int projectId = GetProjectId();

            var task = await _context.Tasks
                .FirstOrDefaultAsync(t => t.Id == id && t.ProjectId == projectId);

            if (task == null) return NotFound(); 

            _context.Tasks.Remove(task);
            await _context.SaveChangesAsync();
            return Ok();
        }

        // --- 5. AJAX API: GET STATS (Workload by User) ---
        [HttpGet]
        public async Task<IActionResult> GetTaskStats()
        {
            int projectId = GetProjectId();

            var stats = await _context.Tasks
                .Where(t => t.ProjectId == projectId && t.Status != "Completed") 
                .Include(t => t.AssignedUser)
                .GroupBy(t => t.AssignedUser) 
                .Select(g => new { 
                    Name = g.Key != null ? g.Key.FullName : "Unassigned",
                    Email = g.Key != null ? g.Key.Email : "", 
                    Count = g.Count() 
                })
                .ToListAsync();

            return Json(stats);
        }

        // --- 6. ADMIN: EDIT PROJECT NAME ---
        [HttpPost]
        public async Task<IActionResult> UpdateProjectName(string newName)
        {
            int projectId = GetProjectId();
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (role != "Admin") return Forbid();

            var project = await _context.Projects.FindAsync(projectId);
            if (project != null && !string.IsNullOrWhiteSpace(newName))
            {
                project.Name = newName;
                await _context.SaveChangesAsync();
            }
            return RedirectToAction(nameof(Index));
        }

        // --- 7. ADMIN: DELETE ENTIRE PROJECT ---
        [HttpPost]
        public async Task<IActionResult> DeleteProject()
        {
            int projectId = GetProjectId();
            var role = User.FindFirst(ClaimTypes.Role)?.Value;
            if (role != "Admin") return Forbid();

            var project = await _context.Projects.FindAsync(projectId);
            
            if (project != null)
            {
                var projectTasks = _context.Tasks.Where(t => t.ProjectId == projectId);
                var projectUsers = _context.Users.Where(u => u.ProjectId == projectId);

                _context.Tasks.RemoveRange(projectTasks);
                _context.Users.RemoveRange(projectUsers); 
                _context.Projects.Remove(project);
                
                await _context.SaveChangesAsync();
            }

            await HttpContext.SignOutAsync("CookieAuth");
            return RedirectToAction("Register", "Account");
        }

        // --- 8. NOTIFICATIONS: MY ALERTS ---
        [HttpGet]
        public async Task<IActionResult> GetMyNotifications()
        {
            int userId = GetUserId();
            var today = DateTime.Today;
            var tomorrow = today.AddDays(1);

            var tasks = await _context.Tasks
                .Where(t => t.AssignedTo == userId && t.Status != "Completed")
                .Where(t => t.DueDate < today || t.DueDate == today || t.DueDate == tomorrow)
                .OrderBy(t => t.DueDate)
                .Select(t => new {
                    t.Title,
                    t.DueDate,
                    IsOverdue = t.DueDate < today,
                    IsDueToday = t.DueDate == today
                })
                .ToListAsync();

            return Json(tasks);
        }

        // --- 9. NOTIFICATIONS: ADMIN OVERDUE ALERTS ---
        [HttpGet]
        public async Task<IActionResult> GetAdminOverdueTasks()
        {
            if (User.FindFirst(ClaimTypes.Role)?.Value != "Admin")
            {
                return Unauthorized();
            }

            int projectId = GetProjectId();
            var today = DateTime.Today;

            var overdueTasks = await _context.Tasks
                .Where(t => t.ProjectId == projectId && t.Status != "Completed" && t.DueDate < today)
                .Include(t => t.AssignedUser)
                .Select(t => new {
                    t.Title,
                    t.DueDate,
                    AssignedToName = t.AssignedUser != null ? t.AssignedUser.FullName : "Unassigned",
                    AssignedToEmail = t.AssignedUser != null ? t.AssignedUser.Email : ""
                })
                .ToListAsync();

            return Json(overdueTasks);
        }

        // --- HELPER METHODS (Defined ONLY ONCE) ---

        private int GetUserId()
        {
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            return claim != null ? int.Parse(claim.Value) : 0;
        }

        private int GetProjectId()
        {
            var claim = User.FindFirst("ProjectId");
            return claim != null ? int.Parse(claim.Value) : 0;
        }
    }
}