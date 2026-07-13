using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using TreeDrive.Infrastructure.Repositories;
using TreeDrive.Infrastructure.Helpers;
using TreeDrive.Shared.DTOs;

namespace TreeDrive.API.Controllers;

[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IConfiguration _configuration;
    private readonly ILogger<AuthController> _logger;
    private readonly UserRepository _userRepository;

    public AuthController(
        IConfiguration configuration, 
        ILogger<AuthController> logger,
        UserRepository userRepository)
    {
        _configuration = configuration;
        _logger = logger;
        _userRepository = userRepository;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new { message = "Username and password required" });
        }

        if (request.Username.Length < 3)
        {
            return BadRequest(new { message = "Username must be at least 3 characters" });
        }

        if (request.Password.Length < 6)
        {
            return BadRequest(new { message = "Password must be at least 6 characters" });
        }

        // Check if user already exists
        if (await _userRepository.UserExistsAsync(request.Username))
        {
            return Conflict(new { message = "Username already exists" });
        }

        // Create new user with hashed password
        var user = new Core.Models.User
        {
            Username = request.Username,
            PasswordHash = PasswordHelper.HashPassword(request.Password),
            Role = "User",
            IsActive = true
        };

        await _userRepository.CreateUserAsync(user);

        _logger.LogInformation("New user registered: {Username}", request.Username);

        return Ok(new { message = "Registration successful. Please login." });
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        if (string.IsNullOrEmpty(request.Username) || string.IsNullOrEmpty(request.Password))
        {
            return BadRequest(new { message = "Username and password required" });
        }

        // Find user in database
        var user = await _userRepository.GetUserByUsernameAsync(request.Username);
        
        if (user == null)
        {
            _logger.LogWarning("Login attempt failed: User {Username} not found", request.Username);
            return Unauthorized(new { message = "Invalid username or password" });
        }

        // Verify password
        if (!PasswordHelper.VerifyPassword(request.Password, user.PasswordHash))
        {
            _logger.LogWarning("Login attempt failed: Invalid password for {Username}", request.Username);
            return Unauthorized(new { message = "Invalid username or password" });
        }

        // Update last login
        await _userRepository.UpdateLastLoginAsync(request.Username);

        // Generate JWT token
        var token = GenerateJwtToken(request.Username, user.Role);
        
        _logger.LogInformation("User logged in: {Username}", request.Username);

        return Ok(new LoginResponse
        {
            Success = true,
            Token = token,
            Username = request.Username,
            Message = "Login successful",
            Role = user.Role
        });
    }

    private string GenerateJwtToken(string username, string role)
    {
        var tokenHandler = new JwtSecurityTokenHandler();
        var key = Encoding.ASCII.GetBytes(_configuration["Jwt:Secret"] ?? "YourSuperSecretKeyHere123!@#$%^&*()");
        var tokenDescriptor = new SecurityTokenDescriptor
        {
            Subject = new ClaimsIdentity(new[]
            {
                new Claim(ClaimTypes.Name, username),
                new Claim(ClaimTypes.Role, role)
            }),
            Expires = DateTime.UtcNow.AddHours(1),
            Issuer = _configuration["Jwt:Issuer"] ?? "TreeDrive",
            Audience = _configuration["Jwt:Audience"] ?? "TreeDriveUsers",
            SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), 
                SecurityAlgorithms.HmacSha256Signature)
        };
        var token = tokenHandler.CreateToken(tokenDescriptor);
        return tokenHandler.WriteToken(token);
    }

    [HttpGet("users/search")]
    public async Task<IActionResult> SearchUsers([FromQuery] string query)
    {
        if (string.IsNullOrEmpty(query) || query.Length < 2)
        {
            return Ok(new { users = new List<string>() });
        }

        var users = await _userRepository.SearchUsersAsync(query);
        return Ok(new { users = users });
    }
}
