using Data_Access_Layer;
using Data_Access_Layer.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Microsoft.IdentityModel.Tokens;
using QuranCenterManager_API.Configuration;
using StudentApi.DTOs.Auth;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;

namespace StudentApi.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        // أقصى عمر مطلق للجلسة من وقت تسجيل الدخول، مهما تكرر التجديد.
        private const int MaxSessionDays = 30;
        private const int RefreshTokenDays = 7;

        private readonly string _jwtSecret;
        private readonly ILogger<AuthController> _logger;
        private readonly UserData _userData;

        public AuthController(
            ILogger<AuthController> logger,
            UserData userData,
            IConfiguration configuration,
            IWebHostEnvironment environment)
        {
            _logger = logger;
            _userData = userData;
            _jwtSecret = JwtConfiguration.GetSecret(configuration, environment);
        }

        [HttpPost("login")]
        [AllowAnonymous]
        [EnableRateLimiting("AuthLimiter")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.Password))
                return Unauthorized("Invalid credentials");

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var email = request.Email.Trim();
            var user = string.IsNullOrWhiteSpace(email)
                ? null
                : await _userData.FindUserForAuthentication(email);

            if (user == null || !user.IsActive || string.IsNullOrWhiteSpace(user.PasswordHash)
                || !BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            {
                _logger.LogWarning("Failed login attempt. Email={Email}, IP={IP}", email, ip);
                return Unauthorized("Invalid credentials");
            }

            if (user.Role == null)
                return Unauthorized("Invalid credentials");

            var sessionStart = DateTime.UtcNow;
            var refreshToken = GenerateRefreshToken(sessionStart);
            user.RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(refreshToken);
            user.RefreshTokenExpiresAt = NextRefreshExpiry(sessionStart);
            user.RefreshTokenRevokedAt = null;
            await _userData.SaveAuthenticationChanges();

            _logger.LogInformation("Successful login. UserId={UserId}, Email={Email}, IP={IP}", user.UserId, user.Email, ip);
            return Ok(new TokenResponse
            {
                AccessToken = CreateAccessToken(user),
                RefreshToken = refreshToken
            });
        }

        [HttpPost("refresh")]
        [AllowAnonymous]
        [EnableRateLimiting("AuthLimiter")]
        public async Task<IActionResult> Refresh([FromBody] RefreshRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.RefreshToken))
                return Unauthorized("Invalid refresh request");

            var ip = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "unknown";
            var email = request.Email.Trim();
            var user = string.IsNullOrWhiteSpace(email)
                ? null
                : await _userData.FindUserForAuthentication(email);

            if (user == null || !user.IsActive || user.RefreshTokenRevokedAt != null
                || user.RefreshTokenExpiresAt == null
                || user.RefreshTokenExpiresAt <= DateTime.UtcNow
                || string.IsNullOrWhiteSpace(user.RefreshTokenHash)
                || !BCrypt.Net.BCrypt.Verify(request.RefreshToken, user.RefreshTokenHash))
            {
                _logger.LogWarning("Invalid refresh attempt. Email={Email}, IP={IP}", email, ip);
                return Unauthorized("Invalid refresh request");
            }

            if (user.Role == null)
                return Unauthorized("Invalid refresh request");

            // بداية الجلسة مدمجة في التوكن (ومشمولة بالتجزئة فلا يمكن العبث بها).
            // التوكنات القديمة بلا بداية تُعامل كجلسة تبدأ الآن.
            var sessionStart = TryGetSessionStart(request.RefreshToken, out var parsedStart)
                ? parsedStart
                : DateTime.UtcNow;

            if (DateTime.UtcNow - sessionStart > TimeSpan.FromDays(MaxSessionDays))
            {
                user.RefreshTokenRevokedAt = DateTime.UtcNow;
                user.RefreshTokenHash = null;
                user.RefreshTokenExpiresAt = null;
                await _userData.SaveAuthenticationChanges();
                _logger.LogInformation("Session reached max age. UserId={UserId}", user.UserId);
                return Unauthorized("Invalid refresh request");
            }

            var newRefreshToken = GenerateRefreshToken(sessionStart);
            user.RefreshTokenHash = BCrypt.Net.BCrypt.HashPassword(newRefreshToken);
            user.RefreshTokenExpiresAt = NextRefreshExpiry(sessionStart);
            user.RefreshTokenRevokedAt = null;
            await _userData.SaveAuthenticationChanges();

            _logger.LogInformation("Refresh succeeded. UserId={UserId}, Email={Email}, IP={IP}", user.UserId, user.Email, ip);
            return Ok(new TokenResponse
            {
                AccessToken = CreateAccessToken(user),
                RefreshToken = newRefreshToken
            });
        }

        [HttpPost("logout")]
        [AllowAnonymous]
        public async Task<IActionResult> Logout([FromBody] LogoutRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Email) || string.IsNullOrWhiteSpace(request.RefreshToken))
                return Ok();

            var email = request.Email.Trim();
            var user = string.IsNullOrWhiteSpace(email)
                ? null
                : await _userData.FindUserForAuthentication(email);

            if (user == null || string.IsNullOrWhiteSpace(user.RefreshTokenHash)
                || !BCrypt.Net.BCrypt.Verify(request.RefreshToken, user.RefreshTokenHash))
            {
                return Ok();
            }

            user.RefreshTokenRevokedAt = DateTime.UtcNow;
            user.RefreshTokenHash = null;
            user.RefreshTokenExpiresAt = null;
            await _userData.SaveAuthenticationChanges();
            return Ok("Logged out successfully");
        }

        private string CreateAccessToken(User user)
        {
            var claims = new List<Claim>
            {
                new(ClaimTypes.NameIdentifier, user.UserId.ToString()),
                new(ClaimTypes.Email, user.Email),
                new(ClaimTypes.Role, user.Role.RoleName),
                new(ClaimTypes.Name, $"{user.FirstName} {user.LastName}".Trim())
            };

            if (user.CenterId.HasValue)
                claims.Add(new Claim("centerId", user.CenterId.Value.ToString()));
            if (user.Teacher != null)
                claims.Add(new Claim("teacherId", user.Teacher.TeacherId.ToString()));
            if (user.Student != null)
                claims.Add(new Claim("studentId", user.Student.StudentId.ToString()));

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_jwtSecret));
            var credentials = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var token = new JwtSecurityToken(
                issuer: "StudentApi",
                audience: "StudentApiUsers",
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(30),
                signingCredentials: credentials
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }

        private static DateTime NextRefreshExpiry(DateTime sessionStartUtc)
        {
            var sliding = DateTime.UtcNow.AddDays(RefreshTokenDays);
            var hardLimit = sessionStartUtc.AddDays(MaxSessionDays);
            return sliding < hardLimit ? sliding : hardLimit;
        }

        // الصيغة: {بداية الجلسة بالثواني}.{عشوائي base64}
        private static string GenerateRefreshToken(DateTime sessionStartUtc)
        {
            var bytes = new byte[64];
            using var rng = RandomNumberGenerator.Create();
            rng.GetBytes(bytes);
            var start = new DateTimeOffset(DateTime.SpecifyKind(sessionStartUtc, DateTimeKind.Utc)).ToUnixTimeSeconds();
            return $"{start}.{Convert.ToBase64String(bytes)}";
        }

        private static bool TryGetSessionStart(string token, out DateTime startUtc)
        {
            startUtc = default;
            var dot = token.IndexOf('.');
            if (dot <= 0 || dot > 12 || !long.TryParse(token[..dot], out var seconds))
                return false;

            try
            {
                startUtc = DateTimeOffset.FromUnixTimeSeconds(seconds).UtcDateTime;
                return true;
            }
            catch (ArgumentOutOfRangeException)
            {
                return false;
            }
        }
    }
}


