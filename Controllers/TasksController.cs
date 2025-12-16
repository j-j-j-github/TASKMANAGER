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
public async Task<IActionResult> GetTasks(string? term)
{
    // 1. Start with "All Tasks"
    // .Include is important if you want to see the Assigned User's name!
    var query = _context.Tasks.Include(t => t.AssignedUser).AsQueryable();

    // 2. Filter by Search Term (if user typed something)
    if (!string.IsNullOrEmpty(term))
    {
        // Search in Title OR Description
        // Force everything to LowerCase so "Task" == "task"
string lowerTerm = term.ToLower(); 

query = query.Where(t => 
    t.Title.ToLower().Contains(lowerTerm) || 
    (t.Description != null && t.Description.ToLower().Contains(lowerTerm))
);
    }

    // 3. Execute Query
    var tasks = await query.Select(t => new {
        t.TaskID,
        t.Title,
        t.Priority,
        // Format date to look nice (yyyy-MM-dd)
        Deadline = t.Deadline.ToString("yyyy-MM-dd"), 
        t.Status,
        t.AssignedTo,
        // If AssignedUser is null, send "Unassigned", else send FullName
        AssignedToName = t.AssignedUser != null ? t.AssignedUser.FullName : "Unassigned"
    }).ToListAsync();

    return Json(tasks);
}
        // POST: /Tasks/SaveTask (Add or Edit)
        // POST: /Tasks/SaveTask (Add or Edit)
[HttpPost]
public async Task<IActionResult> SaveTask([FromBody] TaskItem model)
{
    try
    {
        Console.WriteLine("--- SAVE TASK STARTED ---"); // Debug Log

        // 1. Validate User Session
        var userIdString = User.FindFirstValue(ClaimTypes.NameIdentifier);
        if (string.IsNullOrEmpty(userIdString))
        {
            return StatusCode(401, "Session Expired. Please Logout and Login again.");
        }
        int currentUserId = int.Parse(userIdString);

        // 2. Fix "AssignedTo" (Dropdown sends 0, but DB needs NULL)
        if (model.AssignedTo == 0) 
        {
            model.AssignedTo = null;
        }

        // 3. Save Logic
        if (model.TaskID == 0)
        {
            // NEW TASK
            model.CreatedBy = currentUserId;
            // Ensure we don't accidentally try to insert a whole User object
            model.AssignedUser = null; 
            _context.Tasks.Add(model);
            Console.WriteLine("Adding New Task...");
        }
        else
        {
            // UPDATE TASK
            var task = await _context.Tasks.FindAsync(model.TaskID);
            if (task == null) return NotFound("Task ID not found in database.");

            task.Title = model.Title;
            task.Priority = model.Priority;
            task.Deadline = model.Deadline;
            task.Status = model.Status;
            task.AssignedTo = model.AssignedTo;
            Console.WriteLine("Updating Task ID: " + task.TaskID);
        }

        await _context.SaveChangesAsync();
        Console.WriteLine("--- SUCCESS ---");
        return Json(new { success = true });
    }
    catch (Exception ex)
    {
        // THIS PRINTS THE REAL ERROR TO YOUR TERMINAL
        Console.WriteLine("CRITICAL CRASH: " + ex.Message);
        if (ex.InnerException != null) 
            Console.WriteLine("INNER DETAILS: " + ex.InnerException.Message);

        return StatusCode(500, "Server Error: " + ex.Message);
    }
}

        // POST: /Tasks/DeleteTask
        [HttpPost]
        public async Task<IActionResult> DeleteTask(int id)
        {
            var task = await _context.Tasks.FindAsync(id);
            if (task != null)
            {
                _context.Tasks.Remove(task);
                await _context.SaveChangesAsync();
                return Json(new { success = true });
            }
            return Json(new { success = false });
        }
    }
}