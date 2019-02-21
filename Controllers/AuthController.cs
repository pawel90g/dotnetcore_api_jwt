using Api.Helpers;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace Api.TakesCare.Controllers
{
    [Route("api/[controller]")]
    public class AuthController : Controller
    {
        private readonly UserManager<ApiUser> _userManager;
        private readonly SignInManager<ApiUser> _signinManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly ApiConfig _apiConfig;

        public AuthController(UserManager<ApiUser> userManager, SignInManager<ApiUser> signinManager, RoleManager<IdentityRole> roleManager, IOptions<ApiConfig> apiConfig)
        {
            _userManager = userManager;
            _signinManager = signinManager;
            _roleManager = roleManager;
            _apiConfig = apiConfig.Value;
        }

        /// <summary>
        /// Creates new account
        /// </summary>
        /// <param name="user"></param>
        /// <returns></returns>
        [HttpPost("SignUp")]
        [ProducesResponseType(201)]
        [ProducesResponseType(400)]
        public async Task<IActionResult> SignUp([FromBody] ApiUserDTO user)
        {
            if (ModelState.IsValid && user.Password.Equals(user.ConfirmPassword))
            {
                var apiUser = new ApiUser
                {
                    CreateDate = DateTime.Now,
                    Del = false,
                    Verified = false,
                    Email = user.Email,
                    UserName = user.Email
                };

                var result = await _userManager.CreateAsync(apiUser, user.Password);
                await _userManager.AddToRoleAsync(apiUser, "User");

                if (result.Succeeded)
                {
                    return Ok();
                }

                return BadRequest(result.Errors);
            }

            return BadRequest(ModelState.Values.Select(x => x.Errors));
        }

        /// <summary>
        /// Generates authentication token
        /// </summary>
        /// <returns></returns>
        [HttpPost("SignIn")]
        [ProducesResponseType(200)]
        [ProducesResponseType(401)]
        public async Task<IActionResult> SignIn(string email, string password)
        {
            var signInResult = await _signinManager.PasswordSignInAsync(email, password, false, false);
            if (signInResult.Succeeded)
            {
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null || user.Del || !user.Verified || !user.EmailConfirmed)
                {
                    return Unauthorized();
                }

                var token = await GenerateJwtToken(user);

                return Ok(token);
            }

            return Unauthorized();
        }

        private async Task<string> GenerateJwtToken(ApiUser user)
        {
            var roles = await _userManager.GetRolesAsync(user);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Email),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString()),
                new Claim(ClaimTypes.NameIdentifier, user.Id),
            };

            foreach (var role in roles)
            {
                claims.Add(new Claim(ClaimTypes.Role, role));
            }

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_apiConfig.SecurityKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
            var expires = DateTime.Now.AddHours(12);

            var token = new JwtSecurityToken(
                _apiConfig.JwtIssuer,
                null,
                claims,
                expires: expires,
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }
}