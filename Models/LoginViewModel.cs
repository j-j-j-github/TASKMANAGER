using System.ComponentModel.DataAnnotations;

namespace TaskManagerApp.Models
{
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Please enter your email")]
        public string Email { get; set; }

        [Required(ErrorMessage = "Please enter your password")]
        [DataType(DataType.Password)] 
        public string Password { get; set; }
    }
}