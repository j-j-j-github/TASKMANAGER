using System;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace TaskManagerApp.Models
{
    [Table("Tasks")]
    public class TaskItem
    {
        [Key]
        public int TaskID { get; set; }

        [Required]
        public string Title { get; set; }

        public string? Description { get; set; }
        public string Priority { get; set; }
        public DateTime Deadline { get; set; }
        public string Status { get; set; }
        
        public int? AssignedTo { get; set; }

        [ForeignKey("AssignedTo")]
        public virtual User AssignedUser { get; set; }

        public int CreatedBy { get; set; }
    }
}