using Invoice_and_Payment_System.Models;
using Microsoft.AspNetCore.Identity;

namespace Invoice_and_Payment_System.Data.Seeders
{
    public class AdminSeeder
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly IConfiguration _configuration;


        public AdminSeeder(UserManager<AppUser> userManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _configuration = configuration;
        }

        public async Task SeedAdminAsync()
        {
            var adminEmail = _configuration["Admin:Email"];
            var adminPassword = _configuration["Admin:Password"];

            if (await _userManager.FindByEmailAsync(adminEmail) == null)
            {
                var adminUser = new AppUser
                {
                    UserName = adminEmail,
                    Email = adminEmail,
                    Name = "System Admin",
                };

                await _userManager.CreateAsync(adminUser, adminPassword);
                await _userManager.AddToRoleAsync(adminUser, "Admin");
            }
        }
    }

}
