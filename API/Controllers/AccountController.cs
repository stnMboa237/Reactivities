using System.Security.Claims;
using API.DTOs;
using API.Services;
using Domain;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;

namespace API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AccountController : ControllerBase
    {
        private readonly UserManager<AppUser> _userManager;
        private readonly TokenService _tokenService;
        private readonly IConfiguration _config;
        private readonly HttpClient _httpClient;
        public AccountController(UserManager<AppUser> userManager, TokenService tokenService, IConfiguration config)
        {
            _config = config;
            _tokenService = tokenService;
            _userManager = userManager;
            _httpClient = new HttpClient{
                BaseAddress = new System.Uri("https://graph.facebook.com")
            };
        }

        // we use this method WHEN USER REGISTER, LOGIN OR FACEBOOK_LOGIN
        private async Task SetRefreshToken(AppUser user) {
            // 1 - we generate a new refreshToken
            var refreshToken = _tokenService.GenerateRefreshToken();
            
            // 2 - then we add the new token to the User tokens Collection
            user.RefreshTokens.Add(refreshToken);
            
            // 3 - we save the token to the DB afin que l'user compare le token avec ce qui est stocké en DB
            await _userManager.UpdateAsync(user);

            // 4 - send to token back to the front via a Cookie
            var cookieOption = new CookieOptions {
                HttpOnly = true, // our cookie is accessible ONLY via HTTP and not via javaScript
                Expires = DateTime.UtcNow.AddDays(7), // the refreshToken will expire in 7 days
            };
            
            Response.Cookies.Append("refreshToken", refreshToken.Token, cookieOption);
        }

        [Authorize]
        [HttpPost("refreshToken")]
        public async Task<ActionResult<UserDto>> RefreshToken() {
            var refreshToken = Request.Cookies["refreshToken"];
            var user = await _userManager.Users
                .Include(a => a.RefreshTokens)
                .Include(p => p.Photos)
                .FirstOrDefaultAsync(x => x.UserName == User.FindFirstValue(ClaimTypes.Name));

            if(user == null) {
                return Unauthorized();
            }

            var oldToken = user.RefreshTokens.SingleOrDefault(x => x.Token == refreshToken);

            if(oldToken != null && !oldToken.IsActive) {
                return Unauthorized();
            }

            return CreateUserObject(user);
        } 

        [AllowAnonymous]
        [HttpPost("fbLogin")]
        public async Task<ActionResult<UserDto>> FacebookLogin(string accessToken) {
            var fbVerifyKeys = _config["Facebook:AppId"] + "|" + _config["Facebook:ApiSecret"];

            // verifyTokenResponse: checks if the token send by Facebook is a valid token for our app defined into developers.facebook.com
            var verifyTokenResponse = await _httpClient
                .GetAsync($"debug_token?input_token={accessToken}&access_token={fbVerifyKeys}");

            if(!verifyTokenResponse.IsSuccessStatusCode) {
                return Unauthorized();
            }

            // var fbUrl = "me?access_token="+accessToken+$"&fields=name,email,picture.width(100).height(100)";
            var fbUrl = $"me?access_token={accessToken}&fields=name,email,picture.width(100).height(100)";

            var fbUserInfo = await _httpClient.GetFromJsonAsync<FacebookDto>(fbUrl);

            //check if the facebook user login into the App before
            var user = await _userManager.Users.Include(p => p.Photos)
                .FirstOrDefaultAsync(x => x.Email == fbUserInfo.Email);

            if(user != null) {
                // if the facebook user already exists, so, just return the UserDto object
                return CreateUserObject(user);
            }

            user = new AppUser {
                DisplayName = fbUserInfo.Name,
                Email = fbUserInfo.Email,
                UserName = fbUserInfo.Email,
                Photos = new List<Photo>{
                    new Photo {
                        Id = "fb_" + fbUserInfo.Id,
                        IsMain = true,
                        Url = fbUserInfo.Picture.data.Url,
                    }
                }
            };

            var result = await _userManager.CreateAsync(user);

            if (!result.Succeeded) {
                return BadRequest("Problem creating user account");
            }
            await SetRefreshToken(user);
            return CreateUserObject(user);
        }

        [AllowAnonymous]
        [HttpPost("login")]
        public async Task<ActionResult<UserDto>> Login(LoginDto loginDto)
        {
            var user = await _userManager.Users
                .Include(p => p.Photos)
                .FirstOrDefaultAsync(x => x.Email == loginDto.Email);
                
            if (user == null)
            {
                return Unauthorized();
            }
            var result = await _userManager.CheckPasswordAsync(user, loginDto.Password);
            if (result)
            {
                await SetRefreshToken(user);
                return CreateUserObject(user);
            }
            return Unauthorized();
        }

        [AllowAnonymous]
        [HttpPost("register")]
        public async Task<ActionResult<UserDto>> Register(RegisterDto registerDto)
        {
            if (await _userManager.Users.AnyAsync(x => x.UserName == registerDto.UserName))
            {
                ModelState.AddModelError("Username", "Username is already taken");
                return ValidationProblem();
            }

            if (await _userManager.Users.AnyAsync(x => x.Email == registerDto.Email))
            {
                ModelState.AddModelError("Email", "Email is already taken");
                return ValidationProblem();
            }

            var user = new AppUser
            {
                DisplayName = registerDto.DisplayName,
                Email = registerDto.Email,
                UserName = registerDto.UserName
            };

            var result = await _userManager.CreateAsync(user, registerDto.Password);

            if (result.Succeeded)
            {
                await SetRefreshToken(user);
                return CreateUserObject(user);
            }

            return BadRequest(result.Errors);
        }

        [Authorize]
        [HttpGet]
        public async Task<ActionResult<UserDto>> GetCurrentUser()
        {
            var user = await _userManager.Users
                .Include(p => p.Photos)
                .FirstOrDefaultAsync(x => x.Email == User.FindFirstValue(ClaimTypes.Email));
            await SetRefreshToken(user);
            return CreateUserObject(user);
        }

        private UserDto CreateUserObject(AppUser user)
        {
            return new UserDto
            {
                DisplayName = user.DisplayName,
                Image = user?.Photos?.FirstOrDefault(x => x.IsMain)?.Url,
                Token = _tokenService.CreateToken(user),
                Username = user.UserName
            };
        }
    }
}