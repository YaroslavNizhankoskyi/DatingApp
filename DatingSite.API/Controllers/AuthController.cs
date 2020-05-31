using Microsoft.AspNetCore.Mvc;
using DatingSite.API.Data;
using System.Threading.Tasks;
using DatingSite.API.Models;
using DatingSite.API.Dtos;
using System.Security.Claims;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using Microsoft.Extensions.Configuration;
using System;
using System.IdentityModel.Tokens.Jwt;

namespace DatingSite.API.Controllers {


    [Route ("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase {

        private readonly IAuthRepositry _authRepository;
        private readonly IConfiguration _config;

        public AuthController (IAuthRepositry _authRepository, IConfiguration config) {
            
            this._authRepository = _authRepository;
            _config = config;
        }


        [HttpPost("register")]
        public async Task<IActionResult> Register(UserForRegisterDto userForRegister)
        {
            userForRegister.UserName = userForRegister.UserName.ToLower();

            if(await _authRepository.UserExists(userForRegister.UserName))
                return BadRequest("UserName already exists");

            var UserToCreate = new User
            {
                Username = userForRegister.UserName
            };

            var createdUser = await _authRepository.Register(UserToCreate, userForRegister.Password);

            return StatusCode(201);
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login(UserForLoginDto userForLogin)
        {
            var user = await _authRepository.Login(userForLogin.UserName, userForLogin.Password);

            if(user == null){
                
                return Unauthorized();
            }

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString() ),
                new Claim(ClaimTypes.Name, user.Username)
            };

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config.
                            GetSection("AppSettings:Token").Value));

            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha512Signature);

            var tokenDescriptor = new SecurityTokenDescriptor()
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.Now.AddDays(1),
                SigningCredentials = creds
            };

            var tokenHandler = new JwtSecurityTokenHandler();

            var token = tokenHandler.CreateToken(tokenDescriptor);

            return Ok(new {
                token = tokenHandler.WriteToken(token)
            });                        
        }
    }

    
}