using System.ComponentModel.DataAnnotations;

namespace TaskManagerApp.Models
{
    public class User
    {
        [Key]
        public int Id { get; set; }

        [Required]
        public string FullName { get; set; }

        [Required]
        public string Email { get; set; }

        [Required]
        public string PasswordHash { get; set; }

        public string Role { get; set; } // "Admin" or "User"

        public int ProjectId { get; set; }

        public Project Project { get; set; }
    }
}