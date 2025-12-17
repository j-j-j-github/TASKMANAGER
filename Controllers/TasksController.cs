using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using TaskManagerApp.Models;

namespace TaskManagerApp.Controllers
{
    [Authorize] // 1. Forces user to be logged in to see this
    public class TasksController : Controller
    {
        private readonly AppDbContext _context;

        public TasksController(AppDbContext context)
        {
            _context = context;
        }

        // GET: /Tasks/Index (The Main Dashboard)
        public IActionResult Index()
{
    // We pass the User's ID and Role to the View so JavaScript can use them
    ViewBag.CurrentUserId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier).Value);
    ViewBag.UserRole = User.FindFirst(ClaimTypes.Role).Value;
    
    // CHANGE THIS LINE: Send the full User objects (includes Email)
    ViewBag.UsersList = _context.Users.ToList(); 
    
    return View();
}

        // --- AJAX API (Used by jQuery) ---

        // GET: /Tasks/GetTasks
       [HttpGet]

[HttpGet]
public async Task<IActionResult> GetTasks(string? term)
{
    // 1. Get Current Logged-in User Info
    var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
    var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role);

    int currentUserId = (userIdClaim != null) ? int.Parse(userIdClaim.Value) : 0;
    bool isAdmin = (roleClaim != null) && roleClaim.Value == "Admin";

    // 2. Query Database
    var query = _context.Tasks.Include(t => t.AssignedUser).AsQueryable();

    if (!string.IsNullOrEmpty(term))
    {
        string lowerTerm = term.ToLower();
        query = query.Where(t => 
            t.Title.ToLower().Contains(lowerTerm) || 
            (t.Description != null && t.Description.ToLower().Contains(lowerTerm)) ||
            (t.AssignedUser != null && t.AssignedUser.FullName.ToLower().Contains(lowerTerm))
        );
    }

    // 3. Prepare Data (Add "CanManage" flag)
    var tasks = await query.Select(t => new {
        t.TaskID,
        t.Title,
        t.Description,
        t.Priority,
        Deadline = t.Deadline.ToString("yyyy-MM-dd"), 
        t.Status,
        t.AssignedTo,
        AssignedToName = t.AssignedUser != null ? t.AssignedUser.FullName : "Unassigned",
        AssignedToEmail = t.AssignedUser != null ? t.AssignedUser.Email : "",

        // TRUE if Admin OR if the task belongs to the current user
        CanManage = isAdmin || (t.AssignedTo == currentUserId) 
    }).ToListAsync();

    return Json(tasks);
}

// --- 1. ADMIN: Get list of ALL overdue tasks ---
[HttpGet]
public async Task<IActionResult> GetAdminOverdueTasks()
{
    // specific security check: Only Admin can access this
    var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role);
    if (roleClaim == null || roleClaim.Value != "Admin") return Unauthorized();

    var today = DateTime.Today;

    var overdueTasks = await _context.Tasks
        .Include(t => t.AssignedUser) // We need the user's name
        .Where(t => t.Status != "Completed" && t.Deadline < today) // "Crossed deadline"
        .Select(t => new {
            t.Title,
            Deadline = t.Deadline.ToString("yyyy-MM-dd"),
            AssignedToName = t.AssignedUser != null ? t.AssignedUser.FullName : "Unassigned",
            AssignedToEmail = t.AssignedUser != null ? t.AssignedUser.Email : ""
        })
        .ToListAsync();

    return Json(overdueTasks);
}

// --- 2. NOTIFICATIONS: Get tasks for CURRENT user due today/tomorrow/overdue ---
[HttpGet]
public async Task<IActionResult> GetMyNotifications()
{
    var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
    if (userIdClaim == null) return Json(new List<object>());

    int userId = int.Parse(userIdClaim.Value);
    var today = DateTime.Today;
    var tomorrow = today.AddDays(1);

    var tasks = await _context.Tasks
        .Where(t => t.AssignedTo == userId && t.Status != "Completed")
        .Where(t => t.Deadline <= tomorrow) // Due Today, Tomorrow, or Overdue
        .OrderBy(t => t.Deadline)
        .Select(t => new {
            t.Title,
            Deadline = t.Deadline,
            IsOverdue = t.Deadline < today,
            IsDueToday = t.Deadline == today
        })
        .ToListAsync();

    return Json(tasks);
}

        // POST: /Tasks/SaveTask (Add or Edit)
        // POST: /Tasks/SaveTask (Add or Edit)
[HttpPost]
public async Task<IActionResult> SaveTask([FromBody] TaskItem task)
{
    if (task.TaskID == 0)
    {
        // CREATE NEW TASK (Anyone can create)
        _context.Tasks.Add(task);
    }
    else
    {
        // UPDATE EXISTING TASK (Security Check Needed!)
        var existingTask = await _context.Tasks.FindAsync(task.TaskID);
        if (existingTask == null) return NotFound();

        // 1. Get Current User Info
        var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
        var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role);
        
        if (userIdClaim != null && roleClaim != null)
        {
            int currentUserId = int.Parse(userIdClaim.Value);
            string userRole = roleClaim.Value;

            // 2. Security Check: Block if not Admin AND not Owner
            // (Note: If assignedTo is null/Unassigned, only Admin can edit it)
            bool isOwner = existingTask.AssignedTo == currentUserId;
            
            if (userRole != "Admin" && !isOwner)
            {
                return StatusCode(403, "You can only edit your own tasks!");
            }
        }

        // 3. Apply Updates
        existingTask.Title = task.Title;
        existingTask.Description = task.Description;
        existingTask.Priority = task.Priority;
        existingTask.Deadline = task.Deadline;
        existingTask.Status = task.Status;
        existingTask.AssignedTo = task.AssignedTo;
    }

    await _context.SaveChangesAsync();
    return Ok();
}

        // POST: /Tasks/DeleteTask
        [HttpPost]
public async Task<IActionResult> DeleteTask(int id)
{
    var task = await _context.Tasks.FindAsync(id);
    if (task == null) return NotFound();

    // 1. Get the Current User's Info from the Cookie
    var userIdClaim = User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier);
    var roleClaim = User.FindFirst(System.Security.Claims.ClaimTypes.Role);

    // Safety check: If not logged in, kick them out
    if (userIdClaim == null || roleClaim == null) return Unauthorized();

    int currentUserId = int.Parse(userIdClaim.Value);
    string userRole = roleClaim.Value;

    // 2. The Security Check
    // ALLOW if: User is "Admin" OR User is the "Owner" of the task
    if (userRole == "Admin" || task.AssignedTo == currentUserId)
    {
        _context.Tasks.Remove(task);
        await _context.SaveChangesAsync();
        return Ok();
    }

    // 3. Otherwise, BLOCK THEM
    return StatusCode(403, "You are not authorized to delete this task.");
}
[HttpGet]
public async Task<IActionResult> GetTaskStats()
{
    var stats = await _context.Tasks
        .Where(t => t.Status != "Completed")
        // Group by Email first to be unique
        .GroupBy(t => t.AssignedUser != null ? t.AssignedUser.Email : "Unassigned") 
        .Select(g => new { 
            // We need the Name for the label, take the first one found in the group
            Name = g.First().AssignedUser != null ? g.First().AssignedUser.FullName : "Unassigned",
            Email = g.Key, 
            Count = g.Count() 
        })
        .ToListAsync();

    return Json(stats);
}
    }
}