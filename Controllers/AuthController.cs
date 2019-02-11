using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;

namespace WebApiJwt.Controllers
{
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        private readonly UserManager<ApiUser> _userManager;
        private readonly SignInManager<ApiUser> _signinManager;

        public AuthController(UserManager<ApiUser> userManager, SignInManager<ApiUser> signinManager)
        {
            _userManager = userManager;
            _signinManager = signinManager;
        }

        [HttpPost("SignUp")]
        public async Task<object> SignUp([FromBody] ApiUserDTO user)
        {
            if (ModelState.IsValid && user.Password.Equals(user.ConfirmPassword))
            {
                var result = await _userManager.CreateAsync(new ApiUser
                {
                    CreateDate = DateTime.Now,
                    Del = false,
                    Verified = false,
                    Email = user.Email,
                    UserName = user.Email,
                }, user.Password);


                if (result.Succeeded)
                {
                    return new { registered = true };
                }

                return new { registered = result.Succeeded, errors = result.Errors.Select(x => x.Description) };
            }

            return new { registered = false, errors = ModelState.Values.Select(x => string.Join(", ", x.Errors.Select(y => y.Exception.Message))) };
        }

        [HttpPost("SignIn")]
        public async Task<object> SignIn()
        {
            return await GenerateJwtToken(new IdentityUser { Email = "pawel90g@outlook.com", Id = "pawel90g@outlook.com" });
        }

        private async Task<object> GenerateJwtToken(IdentityUser user)
        {
            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("abc123abc123abc123abc123"));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddHours(12);

            var token = new JwtSecurityToken(
                "test.com",
                "test.com",
                claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}