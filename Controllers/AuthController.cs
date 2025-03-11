using Invoice_and_Payment_System.Data;
using Invoice_and_Payment_System.DTOs;
using Invoice_and_Payment_System.Models;
using Invoice_and_Payment_System.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Identity.UI.Services;
using Microsoft.AspNetCore.Mvc;
using System.Data;
using System.Security.Claims;

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
        private readonly IEmailService _emailService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AppDbContext context, UserManager<AppUser> userManager, SignInManager<AppUser> signInManager, TokenGenerator tokenGenerator, IEmailService emailService, ILogger<AuthController> logger)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _tokenGenerator = tokenGenerator;
            _emailService = emailService;
            _logger = logger;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register(RegisterDTO model)
        {
            _logger.LogInformation("Registration attempt for email: {Email}", model.Email);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid registration model state for {Email}", model.Email);
                return StatusCode(StatusCodes.Status400BadRequest,
                    new { message = "An error occurred during registration", error = "Please try again later" });
            }

            var existingUser = await _userManager.FindByEmailAsync(model.Email);
            if (existingUser != null)
            {
                _logger.LogWarning("Registration attempt with existing email: {Email}", model.Email);
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
                _logger.LogInformation("User registered successfully: {UserId}, {Email}", user.Id, user.Email);
                return StatusCode(StatusCodes.Status201Created, new
                {
                    message = "User registered successfully",
                    user = new { email = user.Email, role = "Customer" }
                });
            }
            else
            {
                _logger.LogWarning("User registration failed for {Email}: {Errors}",
                    model.Email, string.Join(", ", result.Errors.Select(e => e.Description)));
                return BadRequest(new
                {
                    message = "User registration failed",
                    errors = result.Errors.Select(e => e.Description)
                });
            }

        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(LoginDTO model)
        {
            _logger.LogInformation("Login attempt for email: {Email}", model.Email);

            if (!ModelState.IsValid)
            {
                _logger.LogWarning("Invalid login model state for {Email}", model.Email);
                return BadRequest(new
                {
                    message = "Login failed.",
                    errors = ModelState.Values.SelectMany(v => v.Errors).Select(e => e.ErrorMessage)
                });
            }

            var user = await _userManager.FindByEmailAsync(model.Email);

            if (user == null)
            {
                _logger.LogWarning("Login attempt with invalid email: {Email}", model.Email);
                return Unauthorized(new { message = "Invalid email or password." });
            }

            if (user.LockoutEnd != null && user.LockoutEnd > DateTimeOffset.UtcNow)
            {
                _logger.LogWarning("Login attempt for locked account: {UserId}, {Email}", user.Id, user.Email);
                return StatusCode(403, new { message = "Account is temporarily locked. Please try again later." });
            }

            if (!(user.EmailConfirmed))
            {
                _logger.LogWarning("Login attempt for unverified email: {UserId}, {Email}", user.Id, user.Email);
                return BadRequest(new
                {
                    message = "Please verify your email before logging in.",
                    emailVerification = "api/auth/verify-email",
                });
            }

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, lockoutOnFailure: true);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Failed login attempt (invalid password) for user: {UserId}, {Email}",
                    user.Id, user.Email);
                return Unauthorized(new { message = "Invalid email or password." });
            }

            var token = await _tokenGenerator.GenerateToken(user);
            var roles = await _userManager.GetRolesAsync(user);

            _logger.LogInformation("User logged in successfully: {UserId}, {Email}", user.Id, user.Email);
            return Ok(new
            {
                message = "Login successful",
                token = token,
                user = new
                {
                    name = user.Name,
                    email = user.Email,
                    role = roles.FirstOrDefault()
                }
            });

        }

        [HttpPost("verify-email")]
        public async Task<IActionResult> VerifyEmail(VerifyEmailDTO model)
        {
            _logger.LogInformation("Email verification requested for: {Email}", model.Email);

            var user = await _userManager.FindByEmailAsync(model.Email);
            if (user == null)
            {
                _logger.LogWarning("Email verification requested for non-existent user: {Email}", model.Email);
                return BadRequest(new { message = "User not found." });
            }

            if (user.EmailConfirmed)
            {
                _logger.LogInformation("Email verification requested but email already verified: {UserId}, {Email}",
                    user.Id, user.Email);
                return BadRequest(new { message = "Email is already verified." });
            }

            var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
            var verificationLink = $"{Request.Scheme}://{Request.Host}/api/auth/confirm-email?email={model.Email}&token={Uri.EscapeDataString(token)}";

            var subject = "Verify Your Email";
            var body = $"Click the following link to verify your email. This link is valid for 12 hours: <a href='{verificationLink}'>Verify Email</a>";

            try
            {
                await _emailService.SendEmailAsync(model.Email, subject, body);
                _logger.LogInformation("Verification email sent successfully to: {UserId}, {Email}", user.Id, user.Email);
                return Ok(new { message = "Verification email sent." });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to send verification email to: {UserId}, {Email}", user.Id, user.Email);
                return StatusCode(500, new { message = "Failed to send verification email. Please try again later." });
            }
        }

        [HttpGet("confirm-email")]
        public async Task<IActionResult> ConfirmEmail(string email, string token)
        {
            _logger.LogInformation("Email confirmation attempt for: {Email}", email);

            var user = await _userManager.FindByEmailAsync(email);
            if (user == null)
            {
                _logger.LogWarning("Email confirmation attempted for non-existent user: {Email}", email);
                return BadRequest(new { message = "Invalid email." });
            }

            if (user.EmailConfirmed)
            {
                _logger.LogInformation("Email confirmation attempted but email already verified: {UserId}, {Email}",
                    user.Id, email);
                return BadRequest(new { message = "Email is already verified." });
            }

            var result = await _userManager.ConfirmEmailAsync(user, token);
            if (!result.Succeeded)
            {
                _logger.LogWarning("Email confirmation failed with invalid token: {UserId}, {Email}, {Errors}",
                    user.Id, email, string.Join(", ", result.Errors.Select(e => e.Description)));
                return BadRequest(new { message = "Invalid token or email confirmation failed." });
            }

            await _userManager.UpdateAsync(user);
            await _userManager.UpdateSecurityStampAsync(user);

            _logger.LogInformation("Email verified successfully: {UserId}, {Email}", user.Id, email);
            return Ok(new { message = "Email verified successfully. Please login." });
        }

        [Authorize]
        [HttpGet("profile")]
        public async Task<IActionResult> Profile()
        {
            var user = await _userManager.GetUserAsync(User);

            if (user == null)
            {
                _logger.LogWarning("Profile access attempted with valid token but user not found");
                return NotFound(new { message = "User not found." });
            }
            var roles = await _userManager.GetRolesAsync(user);

            if (!user.EmailConfirmed)
            {
                _logger.LogWarning($"Unverified user {user.Id} attempted to access profile");
                return BadRequest(new {
                    message = "Please verify your email.",
                    emailVerification = "api/auth/verify-email",
                });
            }

            _logger.LogInformation($"User {user.Id} accessed their profile");


            return Ok(new
            {
                message = "User profile retrieved successfully",
                user = new
                {
                    id = user.Id,
                    name = user.Name,
                    email = user.Email,
                    role = roles.FirstOrDefault()
                }
            });
        }

    }
}
