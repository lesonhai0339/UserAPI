using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ModelBinding;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Net.Http.Headers;
using System.ComponentModel.DataAnnotations;
using System.Data;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using User.Manager.Service.Models;
using User.Manager.Service.Service;
using UserAPI.Models;
using UserAPI.Models.Authentication.Login;
using UserAPI.Models.Authentication.ResetPassword;
using UserAPI.Models.Authentication.Signup;
using UserAPI.Models.DbContext;

namespace UserAPI.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthenticationController : ControllerBase
    {
        private readonly UserManager<UserAccount> _userManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly SignInManager<UserAccount> _signInManager;
        private readonly IConfiguration _configuration;
        private readonly IEmailService _emailService;
        private readonly IHttpContextAccessor _httpContextAccessor;

        public AuthenticationController(UserManager<UserAccount> userManager,RoleManager<IdentityRole> roleManager,
            IConfiguration configuration,IEmailService emailService, SignInManager<UserAccount> signInManager,IHttpContextAccessor httpContextAccessor) 
        {
            _userManager = userManager;
            _roleManager = roleManager;
            _configuration = configuration;
            _emailService = emailService;
            _signInManager = signInManager;
            _httpContextAccessor=httpContextAccessor;
        }
        [HttpPost]
        [Route("Register")]
        public async Task<IActionResult> Register([FromBody] RegisterUser register,string role)
        {
            var userExist= await _userManager.FindByEmailAsync(register.Email);
            if(userExist!=null)
            {
                return StatusCode(StatusCodes.Status403Forbidden,
                    new Respone { Status = "Error", Message = "User aready exist!" });
            }
            UserAccount user = new()
            {
                Email = register.Email,
                SecurityStamp = Guid.NewGuid().ToString(),
                UserName = register.UserName,
                TwoFactorEnabled = true
            };
            if (await _roleManager.RoleExistsAsync(role))
            {
                var result = await _userManager.CreateAsync(user, register.Password);
                if (!result.Succeeded)
                {
                    return StatusCode(StatusCodes.Status500InternalServerError,
                    new Respone { Status = "Error", Message = "User failed to Create!" });
                }
                await _userManager.AddToRoleAsync(user, role);
                //Ad token and Verified email   
                var token = await _userManager.GenerateEmailConfirmationTokenAsync(user);
                var confirmationLink = Url.Action(nameof(ConfirmEmail), "Authentication", new { token, email = user.Email }, Request.Scheme);
                var message = new Message(new string[] { user.Email! }, "Confirm Email", confirmationLink!);
                 _emailService.SendEmail(message);

                return StatusCode(StatusCodes.Status201Created,
                    new Respone { Status = "Error", Message = "User Create Successfully!" });
            }
            else
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                   new Respone { Status = "Error", Message = "Role Not Exist!" });
            }    
        }

        //[HttpGet]
        //public IActionResult SendEmail()
        //{
        //    var message = new Message(
        //        new string[] {"haileds939@gmail.com"}, //Đối tượng gửi
        //        "Test", //Tiêu đề
        //        "Test sendEmail" //Nội dung
        //       );
        //    _emailService.SendEmail(message);   
        //    return StatusCode(StatusCodes.Status200OK, new Respone
        //    {
        //        Status = "Success",
        //        Message = "Email send Successfully"
        //    });
        //}

        [HttpGet("ConfirmEmail")]
        public async Task<IActionResult> ConfirmEmail(string token,string email)
        {
            var user=await _userManager.FindByEmailAsync(email);
            if (user != null)
            {
                var result=await _userManager.ConfirmEmailAsync(user, token);
                if (result.Succeeded)
                {
                    return StatusCode(StatusCodes.Status200OK,
                        new Respone { Status = "Success", Message = "Email Verified Successfully!" });
                }
            }
            return StatusCode(StatusCodes.Status500InternalServerError,
                        new Respone { Status = "Error", Message = "Email Doesn't Exist!" });
        }

        [HttpPost]
        [Route("Login")]
        public async Task<IActionResult> Login([FromBody] LoginUser loginUser)
        {
            var user = await _userManager.FindByNameAsync(loginUser.UserName);
            //if (user.TwoFactorEnabled)
            //{
            //    await _signInManager.SignOutAsync();
            //    await _signInManager.PasswordSignInAsync(user, loginUser.Password, false, true);
            //    var token = await _userManager.GenerateTwoFactorTokenAsync(user, "Email");

            //    var message = new Message(new string[] { user.Email! }, "Confirm Email", token);
            //    _emailService.SendEmail(message);

            //    return StatusCode(StatusCodes.Status200OK,
            //          new Respone { Status = "Success", Message = $"We have sent and OTP to your email {user.Email}" });
            //}
            if(user != null && await _userManager.CheckPasswordAsync(user,loginUser.Password))
            {
                var authClaim = new List<Claim>
                {
                    new Claim(ClaimTypes.Name,user.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString())
                };
                var userRole = await _userManager.GetRolesAsync(user);
                foreach (var role in userRole)
                {
                    authClaim.Add(new Claim(ClaimTypes.Role, role));
                }
 
                var jwtToken = getToken(authClaim);
                string tokencookie = new JwtSecurityTokenHandler().WriteToken(jwtToken);

                _httpContextAccessor.HttpContext!.Response.Cookies.Append("User_Token", tokencookie,
                    new CookieOptions { Expires = DateTimeOffset.UtcNow.AddMinutes(1) });
                return Ok(new
                {
                    token=new JwtSecurityTokenHandler().WriteToken(jwtToken),
                    expiration= "Token will be expired in: " + jwtToken.ValidTo
                });

            }return Unauthorized();
        }

        [HttpPost]
        [Route("Login-2FA")]
        public async Task<IActionResult> Login2FA(string code,string username)
        {
            var user= await _userManager.FindByNameAsync(username);
            var Signin = await _signInManager.TwoFactorSignInAsync("Email", code, false, false); //Email phải được confirm mới có thể sử dụng
            if (Signin.Succeeded)
            {
                if (user != null)
                {
                    var authClaim = new List<Claim>
                {
                    new Claim(ClaimTypes.Name,user.UserName),
                    new Claim(JwtRegisteredClaimNames.Jti,Guid.NewGuid().ToString())
                };
                    var userRole = await _userManager.GetRolesAsync(user);
                    foreach (var role in userRole)
                    {
                        authClaim.Add(new Claim(ClaimTypes.Role, role));
                    }
                    var jwtToken = getToken(authClaim);
                    return Ok(new
                    {
                        token = new JwtSecurityTokenHandler().WriteToken(jwtToken),
                        expiration = "Token will be expired in: " + jwtToken.ValidTo
                    });
                }
                
            }
            return StatusCode(StatusCodes.Status404NotFound,
                new Respone { Status = "Failed", Message = $"Invalid Code" });

        }
        [HttpGet]
        [Route("Logout")]
        public async Task<IActionResult> LogOut()
        {
            await _signInManager.SignOutAsync();
            return StatusCode(StatusCodes.Status200OK,
                     new Respone { Status = "Success", Message = $"Logout Successfully!" });
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("Forgot-password")]
        public async Task<IActionResult> ForgotPassword([Required]string email)
        {
            var user = await _userManager.FindByEmailAsync(email);
            if (email != null)
            {
                var token = await _userManager.GeneratePasswordResetTokenAsync(user);
                var forgotPasswordLink = Url.Action(nameof(Resetpassword), "Authentication", new { token, email = user.Email }, Request.Scheme);
                var message = new Message(new string[] { user.Email! }, "Forgot Passwrd Link", forgotPasswordLink!);
                _emailService.SendEmail(message);
                return StatusCode(StatusCodes.Status200OK,
                    new Respone { Status = "Success", Message = $"Password Changed Request Is Sent On Email {user.Email}. Please Open Your Email & Changed New Password" });
            }
            return StatusCode(StatusCodes.Status400BadRequest,
                    new Respone { Status = "Errr", Message = $"Couldn't send to email. Please truy again!" });
        }
        [HttpGet("Reset-password")]
        public IActionResult Resetpassword(string token,string email)
        {
            var model=new ResetPassword { token=token, email=email };
            return Ok(new
            {
                model
            });
        }
        [HttpPost]
        [AllowAnonymous]
        [Route("Reset-password")]
        public async Task<IActionResult> Resetpassword(ResetPassword reset)
        {
            var user = await _userManager.FindByEmailAsync(reset.email);
            if (user != null)
            {
                var resetResult = await _userManager.ResetPasswordAsync(user, reset.token, reset.password);
                if(!resetResult.Succeeded)
                {
                    foreach (var error in resetResult.Errors)
                    {
                        ModelState.AddModelError(error.Code, error.Description);
                    }
                    return Ok(ModelState);
                }
                return StatusCode(StatusCodes.Status200OK,
                    new Respone { Status = "Success", Message = $"Password Changed Request Is Sent On Email {user.Email}. Please Open Your Email & Changed New Password" });
            }
            return StatusCode(StatusCodes.Status400BadRequest,
                    new Respone { Status = "Errr", Message = $"Couldn't send to email. Please truy again!" });
        }
        private JwtSecurityToken getToken(List<Claim> authClaims)
        {
            var authSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_configuration["JWT:Secret"]));

            var token = new JwtSecurityToken(
                issuer: _configuration["JWT:ValidIssuer"],
                audience: _configuration["JWT:ValidAudience"],
                expires:DateTime.UtcNow.AddMinutes(1),
                claims:authClaims,
                signingCredentials:new SigningCredentials(authSigningKey,SecurityAlgorithms.HmacSha256)
                );
            return token;
        }
    }
}
