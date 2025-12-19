using System.ComponentModel.DataAnnotations;

namespace TaskManagerApp.Models
{
    public class Project
    {
        public int Id { get; set; }

        [Required]
        public string Name { get; set; } = string.Empty; // Fixes warning

        public string InviteCode { get; set; } = string.Empty; // Fixes warning
    }
}