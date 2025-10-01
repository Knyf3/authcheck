using authcheck.Model;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace authcheck.Controllers
{

    [ApiController]
    [Route("api/[controller]")]
    //[Authorize(Roles = "Admin")] // Only allow admins to delete

    public class UserController : ControllerBase
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly IConfiguration _configuration;

        public UserController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager, IConfiguration configuration)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _configuration = configuration;
        }
        // DELETE: api/User/{username}
        [HttpDelete("{userName}")]
        public async Task<IActionResult> DeleteUser(string userName)
        {
            var user = await _userManager.FindByNameAsync(userName);
            if (user == null)
                return NotFound(new { message = "User not found." });

            // Optional: Prevent self-deletion
            if (User?.Identity?.Name == user.UserName)
                return BadRequest(new { message = "You cannot delete your own account." });

            var result = await _userManager.DeleteAsync(user);
            if (result.Succeeded)
                return Ok(new { message = $"User {userName} deleted successfully." });

            return BadRequest(result.Errors);
        }

        // POST: api/User/register
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] UserModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = new IdentityUser
            {
                UserName = model.UserName,
                Email = model.Email
            };

            var result = await _userManager.CreateAsync(user, model.Password);

            if (result.Succeeded)
                return Ok(new { message = "User registered successfully." });

            return BadRequest(result.Errors);
        }


        // POST: api/User/login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] UserLoginModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            //var result = await _signInManager.PasswordSignInAsync(
            //    model.UserName,
            //    model.Password,
            //    isPersistent: false,
            //    lockoutOnFailure: false);

            //if (result.Succeeded)
            //    return Ok(new { message = "Login successful." });

            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user == null)
                return Unauthorized(new { message = "Invalid username or password." });

            var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
            if (!result.Succeeded)
                return Unauthorized(new { message = "Invalid username or password." });

            // Generate JWT token
            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.UserName),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["Jwt:Key"] ?? "your_secret_key_here"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var token = new JwtSecurityToken(
                issuer: _configuration["Jwt:Issuer"] ?? "authcheck",
                audience: null,
                claims: claims,
                expires: DateTime.UtcNow.AddHours(1),
                signingCredentials: creds);

            var tokenString = new JwtSecurityTokenHandler().WriteToken(token);

            return Ok(new { token = tokenString });
        }

        // POST: api/User/change-password
        //[Authorize]
        [HttpPost("change-password")]
        public async Task<IActionResult> ChangePassword([FromBody] UserLoginModel model)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var user = await _userManager.FindByNameAsync(model.UserName);
            if (user == null)
                return Unauthorized(new { message = "User not found." });

            var resetToken = await _userManager.GeneratePasswordResetTokenAsync(user);
            var result = await _userManager.ResetPasswordAsync(user, resetToken, model.Password);

            if (result.Succeeded)
                return Ok(new { message = "Password changed successfully." });

            return BadRequest(result.Errors);
        }

    }
}
