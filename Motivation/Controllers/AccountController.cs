using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Motivation.Data;
using Motivation.Models;
using Motivation.Options;
using Newtonsoft.Json;

namespace Motivation.Controllers
{
    public class AccountController : Controller
    {
        private readonly UserManager<IdentityUser> _userManager;
        private readonly SignInManager<IdentityUser> _signInManager;
        private readonly RoleManager<IdentityRole> _roleManager;
        private readonly IEmployeesRepository _employeesRepository;

        public AccountController(
            UserManager<IdentityUser> userManager,
            SignInManager<IdentityUser> signInManager,
            RoleManager<IdentityRole> roleManager,
            IEmployeesRepository employeesRepository
        )
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _roleManager = roleManager;
            _employeesRepository = employeesRepository;

            IdentitySeedData.EnsurePopulated(_userManager, _roleManager).Wait();
        }

        [AllowAnonymous]
        public ViewResult Login()
        {
            return View();
        }

        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginModel loginModel)
        {
            if (ModelState.IsValid)
            {
                var user = await _userManager.FindByEmailAsync(loginModel.Email);
                if (user != null)
                {
                    await _signInManager.SignOutAsync();
                    if (
                        (
                            await _signInManager.PasswordSignInAsync(
                                user,
                                loginModel.Password,
                                loginModel.RememberMe,
                                false
                            )
                        ).Succeeded
                    )
                    {
                        return Redirect("/Statistics");
                    }
                }
            }
            ModelState.AddModelError("", "Неверное имя пользователя или пароль");
            return View(loginModel);
        }

        [HttpPost]
        [Route("Account/ConnectBitrix")]
        [ApiVersion("1.0")]
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        public async Task<IActionResult> ConnectBitrix([FromBody] BitrixUser req)
        {
            try
            {
                var userEmail = User.Claims.FirstOrDefault(i => i.Type == ClaimTypes.Name);
                if (userEmail == null)
                    return BadRequest();

                var user = await _userManager.FindByEmailAsync(userEmail.Value);
                if (user == null)
                    return BadRequest();

                var employee = await _employeesRepository.Entries.FirstOrDefaultAsync(e =>
                    e.UserId == user.Id
                );
                if (employee == null)
                    return BadRequest();

                employee.BitrixUserId = req.Id;

                await _employeesRepository.UpdateAsync(employee);
                return Ok();
            }
            catch (Exception e)
            {
                var exceptionString = $"Error occured while trying to login via Bitrix:\n {e}";
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                var json = JsonConvert.SerializeObject(new { message = exceptionString });
                await Response.WriteAsync(json);
                return StatusCode(500);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("Account/LoginApp")]
        [ApiVersion("1.0")]
        public async Task LoginApp([FromBody] LoginModel loginModel)
        {
            try
            {
                var email = loginModel.Email;
                var password = loginModel.Password;

                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                    throw new Exception("Invalid user or password");

                var isGood = await _userManager.CheckPasswordAsync(user, password);
                if (!isGood)
                    throw new Exception("Invalid user or password");

                var claims = new List<Claim>();

                if (user.Email != null)
                {
                    claims.Add(new Claim(ClaimsIdentity.DefaultNameClaimType, user.Email));
                }

                var userRoles = await _userManager.GetRolesAsync(user);
                foreach (var userRole in userRoles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, userRole));
                }

                var accessClaimsIdentity = new ClaimsIdentity(
                    claims,
                    "Token",
                    ClaimsIdentity.DefaultNameClaimType,
                    ClaimsIdentity.DefaultRoleClaimType
                );

                var roleId = 0;
                if (userRoles.Contains("Managers"))
                    roleId = 1;
                if (userRoles.Contains("Admins"))
                    roleId = 2;

                var accessToken = new JwtSecurityToken(
                    issuer: AuthOptions.Issuer,
                    audience: AuthOptions.Audience,
                    notBefore: DateTime.UtcNow,
                    claims: accessClaimsIdentity.Claims,
                    expires: DateTime.UtcNow.Add(TimeSpan.FromSeconds(AuthOptions.RefreshLifetime)),
                    signingCredentials: new SigningCredentials(
                        AuthOptions.GetSymmetricAccessSecurityKey(),
                        SecurityAlgorithms.HmacSha256
                    )
                );

                var encodedAccessToken = new JwtSecurityTokenHandler().WriteToken(accessToken);

                var refreshClaimsIdentity = new ClaimsIdentity(
                    claims,
                    "Token",
                    ClaimsIdentity.DefaultNameClaimType,
                    ClaimsIdentity.DefaultRoleClaimType
                );
                var refreshToken = new JwtSecurityToken(
                    issuer: AuthOptions.Issuer,
                    audience: AuthOptions.Audience,
                    notBefore: DateTime.UtcNow,
                    claims: refreshClaimsIdentity.Claims,
                    expires: DateTime.UtcNow.Add(TimeSpan.FromSeconds(AuthOptions.RefreshLifetime)),
                    signingCredentials: new SigningCredentials(
                        AuthOptions.GetSymmetricRefreshSecurityKey(),
                        SecurityAlgorithms.HmacSha256
                    )
                );

                var encodedRefreshToken = new JwtSecurityTokenHandler().WriteToken(refreshToken);

                var employee = await _employeesRepository.Entries.FirstOrDefaultAsync(e =>
                    e.UserId == user.Id
                );

                var response = new
                {
                    access_token = encodedAccessToken,
                    refresh_token = encodedRefreshToken,
                    role_id = roleId,
                    user_id = employee?.Id,
                };

                var json = JsonConvert.SerializeObject(response);
                await Response.WriteAsync(json);
            }
            catch (Exception e)
            {
                var exceptionString = $"Error occured while trying to login:\n {e}";
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                var json = JsonConvert.SerializeObject(new { message = exceptionString });
                await Response.WriteAsync(json);
            }
        }

        [HttpPost]
        [Route("Account/Refresh")]
        [ApiVersion("1.0")]
        public async Task Refresh([FromBody] TokenRequest tokenRequest)
        {
            try
            {
                var jwtTokenHandler = new JwtSecurityTokenHandler();
                var validationResult = await jwtTokenHandler.ValidateTokenAsync(
                    tokenRequest.Token,
                    new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidIssuer = AuthOptions.Issuer,
                        ValidateAudience = true,
                        ValidAudience = AuthOptions.Audience,
                        ValidateLifetime = false,
                        IssuerSigningKey = AuthOptions.GetSymmetricRefreshSecurityKey(),
                    }
                );
                if (!validationResult.IsValid)
                {
                    throw new Exception("Invalid refresh token");
                }

                var userEmail = validationResult.ClaimsIdentity.Claims.FirstOrDefault(i =>
                    i.Type == ClaimTypes.Name
                );
                if (userEmail == null)
                    return;

                var user = await _userManager.FindByEmailAsync(userEmail.Value);
                if (user == null)
                    return;

                var employee = await _employeesRepository.Entries.FirstOrDefaultAsync(e =>
                    e.UserId == user.Id
                );

                var claims = new List<Claim>();

                if (user.Email != null)
                {
                    claims.Add(new Claim(ClaimsIdentity.DefaultNameClaimType, user.Email));
                }

                var userRoles = await _userManager.GetRolesAsync(user);
                foreach (var userRole in userRoles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, userRole));
                }

                var accessClaimsIdentity = new ClaimsIdentity(
                    claims,
                    "Token",
                    ClaimsIdentity.DefaultNameClaimType,
                    ClaimsIdentity.DefaultRoleClaimType
                );

                var roleId = 0;
                if (userRoles.Contains("Managers"))
                    roleId = 1;
                if (userRoles.Contains("Admins"))
                    roleId = 2;

                var accessToken = new JwtSecurityToken(
                    issuer: AuthOptions.Issuer,
                    audience: AuthOptions.Audience,
                    notBefore: DateTime.UtcNow,
                    claims: accessClaimsIdentity.Claims,
                    expires: DateTime.UtcNow.Add(TimeSpan.FromSeconds(AuthOptions.RefreshLifetime)),
                    signingCredentials: new SigningCredentials(
                        AuthOptions.GetSymmetricAccessSecurityKey(),
                        SecurityAlgorithms.HmacSha256
                    )
                );

                var encodedAccessToken = new JwtSecurityTokenHandler().WriteToken(accessToken);

                var refreshClaimsIdentity = new ClaimsIdentity(
                    claims,
                    "Token",
                    ClaimsIdentity.DefaultNameClaimType,
                    ClaimsIdentity.DefaultRoleClaimType
                );
                var refreshToken = new JwtSecurityToken(
                    issuer: AuthOptions.Issuer,
                    audience: AuthOptions.Audience,
                    notBefore: DateTime.UtcNow,
                    claims: refreshClaimsIdentity.Claims,
                    expires: DateTime.UtcNow.Add(TimeSpan.FromSeconds(AuthOptions.RefreshLifetime)),
                    signingCredentials: new SigningCredentials(
                        AuthOptions.GetSymmetricRefreshSecurityKey(),
                        SecurityAlgorithms.HmacSha256
                    )
                );

                var encodedRefreshToken = new JwtSecurityTokenHandler().WriteToken(refreshToken);

                var response = new
                {
                    access_token = encodedAccessToken,
                    refresh_token = encodedRefreshToken,
                    role_id = roleId,
                    user_id = employee?.Id,
                };

                var json = JsonConvert.SerializeObject(response);
                await Response.WriteAsync(json);
            }
            catch (Exception e)
            {
                var exceptionString = $"Error occured while trying to login:\n {e}";
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                var json = JsonConvert.SerializeObject(new { message = exceptionString });
                await Response.WriteAsync(json);
            }
        }

        [HttpPost]
        [AllowAnonymous]
        [Route("Account/LoginMobile")]
        [ApiVersion("1.0")]
        public async Task LoginMobile([FromBody] LoginModel loginModel)
        {
            try
            {
                var email = loginModel.Email;
                var password = loginModel.Password;

                ClaimsIdentity? claimsIdentity = null;
                var user = await _userManager.FindByEmailAsync(email);
                if (user == null)
                    throw new Exception("Invalid user or password");

                var isGood = await _userManager.CheckPasswordAsync(user, password);
                if (!isGood)
                    throw new Exception("Invalid user or password");

                var claims = new List<Claim>();

                if (user.Email != null)
                {
                    claims.Add(new Claim(ClaimsIdentity.DefaultNameClaimType, user.Email));
                }

                var userRoles = await _userManager.GetRolesAsync(user);
                foreach (var userRole in userRoles)
                {
                    claims.Add(new Claim(ClaimTypes.Role, userRole));
                }

                claimsIdentity = new ClaimsIdentity(
                    claims,
                    "Token",
                    ClaimsIdentity.DefaultNameClaimType,
                    ClaimsIdentity.DefaultRoleClaimType
                );

                var roleId = 0;
                if (userRoles.Contains("Managers"))
                    roleId = 1;
                if (userRoles.Contains("Admins"))
                    roleId = 2;

                var jwt = new JwtSecurityToken(
                    issuer: AuthOptions.Issuer,
                    audience: AuthOptions.Audience,
                    notBefore: DateTime.UtcNow,
                    claims: claimsIdentity.Claims,
                    expires: DateTime.UtcNow.Add(TimeSpan.FromMinutes(AuthOptions.LifetimeMinutes)),
                    signingCredentials: new SigningCredentials(
                        AuthOptions.GetSymmetricAccessSecurityKey(),
                        SecurityAlgorithms.HmacSha256
                    )
                );

                var encodedJwt = new JwtSecurityTokenHandler().WriteToken(jwt);

                var employee = await _employeesRepository.Entries.FirstOrDefaultAsync(e =>
                    e.UserId == user.Id
                );

                var response = new
                {
                    access_token = encodedJwt,
                    role_id = roleId,
                    user_id = employee?.Id,
                };

                var json = JsonConvert.SerializeObject(response);
                await Response.WriteAsync(json);
            }
            catch (Exception e)
            {
                var exceptionString = $"Error occured while trying to login:\n {e}";
                Response.StatusCode = StatusCodes.Status500InternalServerError;
                var json = JsonConvert.SerializeObject(new { message = exceptionString });
                await Response.WriteAsync(json);
            }
        }

        public async Task<RedirectResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return Redirect("/Account/Login");
        }

        public ActionResult AccessDenied()
        {
            return StatusCode(403);
        }
    }
}
