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
            ViewBag.CurrentUserId = int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier));
            ViewBag.UserRole = User.FindFirstValue(ClaimTypes.Role);
            
            // We also pass a list of users (for the "Assign To" dropdown)
            ViewBag.UsersList = _context.Users.Select(u => new { u.UserID, u.FullName }).ToList();
            
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
        
        // TRUE if Admin OR if the task belongs to the current user
        CanManage = isAdmin || (t.AssignedTo == currentUserId) 
    }).ToListAsync();

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
        // 1. Exclude Completed tasks (Show only active workload)
        .Where(t => t.Status != "Completed")
        // 2. Group by the User's Name
        .GroupBy(t => t.AssignedUser != null ? t.AssignedUser.FullName : "Unassigned")
        // 3. Count them
        .Select(g => new { 
            Name = g.Key, 
            Count = g.Count() 
        })
        .ToListAsync();

    return Json(stats);
}
    }
}