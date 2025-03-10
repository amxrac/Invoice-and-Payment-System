using Microsoft.AspNetCore.Identity;
using System.ComponentModel.DataAnnotations;

namespace Invoice_and_Payment_System.Models
{
    public class AppUser : IdentityUser
    {
        [Required]
        [MaxLength(100)]
        public string Name { get; set; }
    }
}
