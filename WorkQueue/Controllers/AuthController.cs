using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using WorkQueue.Infrastructure;
using Microsoft.EntityFrameworkCore;

namespace WorkQueue.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly WorkQueueDbContext _context;
        private readonly IConfiguration _configuration;

        public AuthController(WorkQueueDbContext context, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var user = await _context.Users
                .IgnoreQueryFilters() //!!!!!
                .Include(u => u.Organization)
                .FirstOrDefaultAsync(u => u.Email == request.Email);

            if (user == null)
            {
                return Unauthorized("Неверный email или пароль");
            }

            // Проверяем пароль (используем тот же метод хэширования, что и в сидинге)
            var hashedPassword = HashPassword(request.Password);
            if (user.PasswordHash != hashedPassword)
            {
                return Unauthorized("Неверный email или пароль");
            }

            // Формируем Claims (данные, зашитые внутри токена)
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
                new Claim("OrganizationId", user.OrganizationId.ToString()),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, user.Email),
                new Claim("Name", user.Name)
            };

            // Генерируем токен
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"]!));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"],
                audience: _configuration["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddHours(2), // Токен живет 2 часа
                signingCredentials: creds
            );

            // ТЗ требует вернуть токен и базовый профиль юзера
            return Ok(new
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                UserProfile = new
                {
                    user.Id,
                    user.Name,
                    user.Email,
                    user.Role,
                    OrganizationId = user.OrganizationId,
                    OrganizationName = user.Organization?.Name
                }
            });
        }

        // Вспомогательный метод для хэширования пароля (должен совпадать с логикой DataSeeder)
        private string HashPassword(string password)
        {
            using var sha256 = SHA256.Create();
            var bytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(password));
            return Convert.ToBase64String(bytes);
        }
    }

    // DTO для принятия данных логина
    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
