using Invoice_and_Payment_System.Data;
using Invoice_and_Payment_System.DTOs;
using Invoice_and_Payment_System.Models;
using Invoice_and_Payment_System.Services;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;

namespace Invoice_and_Payment_System.Controllers
{
    [ApiController]
    [Route("api/[Controller]")]
    public class AuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly UserManager<AppUser> _userManager;
        private readonly SignInManager<AppUser> _signInManager;
        private readonly TokenGenerator _tokenGenerator;

        public AuthController(AppDbContext context, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, TokenGenerator tokenGenerator)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenGenerator = tokenGenerator;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDTO model)
        {
            if (ModelState.IsValid)
            {
                var existingUser = await _userManager.FindByEmailAsync(model.Email);
                if (existingUser != null)
                {
                    return Conflict(new { message = "Email already registered" });
                }
                AppUser user = new()
                {
                    Name = model.Name,
                    Email = model.Email,
                    UserName = model.Email
                };
                
                var result = await _userManager.CreateAsync(user, model.Password!);
                if (result.Succeeded)
                {
                    await _userManager.AddToRoleAsync(user, "Customer");
                    return StatusCode(StatusCodes.Status201Created, new
                    { message = "User registered successfully",
                      user = new { email = user.Email, 
                                    role = "Customer" } 
                    });
                }
                else
                {
                    return BadRequest(new
                    {
                        message = "User registration failed",
                        errors = result.Errors.Select(e => e.Description)
                    });
                }
            }
            return StatusCode(StatusCodes.Status400BadRequest,
            new
            { message = "An error occurred during registration", error = "Please try again later"
            });

        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDTO model)
        {
            if (!ModelState.IsValid)
            {
                return BadRequest(new
                {
                    message = "Request failed.",
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });
            }

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            if (user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow)
            {
                return StatusCode(403, new { message = "Account is temporarily locked. Please try again later." });
            }
                        
            if (!(user.EmailConfirmed))
            {
                return BadRequest(new
                {
                    message = "Please verify your email or phone number before logging in.",
                    emailVerification = "api/auth/verify-email",
                    phoneVerification = "api/auth/verify-phone"
                });
            }


            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
            if (!result.Succeeded)
            {
                return Unauthorized(new { message = "Invalid email or password." });
            }

            var token = await _tokenGenerator.GenerateToken(user);
            var role = await _userManager.GetRolesAsync(user);
            return Ok(new
            {
                message = "Login successful",
                token = token,
                user = new
                {
                    name = user.Name,
                    email = user.Email,
                    role = role
                }
            });

        }


    }
}
