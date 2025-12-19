using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagerApp.Models
{
    [Table("Tasks")]
    public class TaskItem
    {
        // 1. Renamed 'TaskID' to 'Id' to match the Controller
        [Key]
        public int Id { get; set; }

        [Required]
        public string Title { get; set; } = string.Empty; // Fixes "Non-nullable" warning

        public string? Description { get; set; }
        
        // kept your Priority field
        public string Priority { get; set; } = "Medium"; 

        // 2. Renamed 'Deadline' to 'DueDate' to match the Controller
        public DateTime DueDate { get; set; }
        
        public string Status { get; set; } = "Todo";

        // The Security Badge (Required for multi-tenant)
        public int ProjectId { get; set; }
        
        // Navigation Properties (Kept these for future use)
        public int? AssignedTo { get; set; }
        
        [ForeignKey("AssignedTo")]
        public virtual User? AssignedUser { get; set; }

        public int CreatedBy { get; set; }
    }
}