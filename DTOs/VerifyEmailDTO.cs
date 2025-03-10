using System.ComponentModel.DataAnnotations;

namespace Invoice_and_Payment_System.DTOs
{
    public class VerifyEmailDTO
    {
        [Required]
        [EmailAddress]
        [StringLength(255, ErrorMessage = "Email cannot exceed 255 characters.")]
        public string Email { get; set; }
    }
}
