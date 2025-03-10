using System.ComponentModel.DataAnnotations;

namespace Invoice_and_Payment_System.DTOs
{
    public class LoginDTO
    {
        [Required]
        [EmailAddress]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters.")]
        public string Email { get; set; }

        [Required]
        [DataType(DataType.Password)]
        [StringLength(100, MinimumLength = 6, ErrorMessage = "Password must be at least 8 characters.")]
        public string Password { get; set; }
    }
}
