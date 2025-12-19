using System.ComponentModel.DataAnnotations;

namespace TaskManagerApp.Models
{
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Full Name is required")]
        public string FullName { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Password is required")]
        [DataType(DataType.Password)]
        public string Password { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare("Password", ErrorMessage = "Passwords do not match")]
        public string ConfirmPassword { get; set; } = string.Empty;

        // Team Logic
        public bool IsCreatingTeam { get; set; } 
        public string? TeamName { get; set; } 
        public string? InviteCode { get; set; }
    }
}