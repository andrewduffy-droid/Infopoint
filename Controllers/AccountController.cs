using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Google;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using InfoPoint.Models;

namespace InfoPoint.Controllers
{
    public class AccountController : Controller
    {
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            SignInManager<ApplicationUser> signInManager,
            UserManager<ApplicationUser> userManager,
            ILogger<AccountController> logger)
        {
            _signInManager = signInManager;
            _userManager = userManager;
            _logger = logger;
        }

        [HttpGet]
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;
            return View();
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> SwitchAccount(string? returnUrl = null)
        {
            // Clear all authentication cookies
            await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            await HttpContext.SignOutAsync();
            
            // Clear the authentication cookie
            Response.Cookies.Delete(".AspNetCore.Identity.Application");
            Response.Cookies.Delete(".AspNetCore.Identity.External");
            
            // Redirect to force new Google login with account selection
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { ReturnUrl = returnUrl });
            var properties = new AuthenticationProperties { RedirectUri = redirectUrl };
            
            // Force account selection parameters
            properties.SetParameter("prompt", "select_account");
            properties.SetParameter("access_type", "online");
            properties.SetParameter("max_age", "0");
            properties.SetParameter("hd", "*");
            
            return Challenge(properties, GoogleDefaults.AuthenticationScheme);
        }

        [HttpPost]
        [AllowAnonymous]
        public IActionResult ExternalLogin(string provider, string? returnUrl = null)
        {
            // Request a redirect to the external login provider
            var redirectUrl = Url.Action(nameof(ExternalLoginCallback), "Account", new { ReturnUrl = returnUrl });
            var properties = _signInManager.ConfigureExternalAuthenticationProperties(provider, redirectUrl);
            
            // Force account selection for Google OAuth
            if (provider == GoogleDefaults.AuthenticationScheme)
            {
                // Force Google to show account chooser
                properties.SetParameter("prompt", "select_account");
                properties.SetParameter("access_type", "online");
                
                // Clear any login hint to prevent auto-selection
                properties.Items.Remove("login_hint");
                
                // Add parameter to show account chooser even if only one account
                properties.SetParameter("hd", "*");
                
                // Force re-authentication
                properties.SetParameter("max_age", "0");
            }
            
            return Challenge(properties, provider);
        }

        [HttpGet]
        [AllowAnonymous]
        public async Task<IActionResult> ExternalLoginCallback(string? returnUrl = null, string? remoteError = null)
        {
            returnUrl ??= Url.Content("~/");
            
            if (remoteError != null)
            {
                TempData["ErrorMessage"] = $"Error from external provider: {remoteError}";
                return RedirectToAction(nameof(Login));
            }

            var info = await _signInManager.GetExternalLoginInfoAsync();
            if (info == null)
            {
                TempData["ErrorMessage"] = "Error loading external login information.";
                return RedirectToAction(nameof(Login));
            }

            // Check if email domain is @g.bdc.ac.uk
            var email = info.Principal.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email) || !email.EndsWith("@g.bdc.ac.uk"))
            {
                await HttpContext.SignOutAsync();
                TempData["ErrorMessage"] = "Access denied. Only staff with @g.bdc.ac.uk email addresses are allowed.";
                return RedirectToAction(nameof(Login));
            }

            // Sign in the user with this external login provider if the user already has a login
            var result = await _signInManager.ExternalLoginSignInAsync(info.LoginProvider, info.ProviderKey, isPersistent: false, bypassTwoFactor: true);
            if (result.Succeeded)
            {
                _logger.LogInformation("User logged in with {Provider} provider.", info.LoginProvider);
                
                // Update last login date
                var user = await _userManager.FindByEmailAsync(email);
                if (user != null)
                {
                    user.LastLoginDate = DateTime.UtcNow;
                    await _userManager.UpdateAsync(user);
                }
                
                return LocalRedirect(returnUrl);
            }
            
            if (result.IsLockedOut)
            {
                return RedirectToAction("Lockout");
            }
            else
            {
                // If the user does not have an account, create one
                return await CreateUserFromExternalProvider(info, email, returnUrl);
            }
        }

        private async Task<IActionResult> CreateUserFromExternalProvider(ExternalLoginInfo info, string email, string returnUrl)
        {
            var user = new ApplicationUser
            {
                UserName = email,
                Email = email,
                FullName = info.Principal.FindFirstValue(ClaimTypes.Name),
                GoogleId = info.Principal.FindFirstValue(ClaimTypes.NameIdentifier),
                DateCreated = DateTime.UtcNow,
                LastLoginDate = DateTime.UtcNow,
                EmailConfirmed = true // Auto-confirm email for Google accounts
            };

            var createResult = await _userManager.CreateAsync(user);
            if (createResult.Succeeded)
            {
                createResult = await _userManager.AddLoginAsync(user, info);
                if (createResult.Succeeded)
                {
                    _logger.LogInformation("User created an account using {Provider} provider.", info.LoginProvider);
                    await _signInManager.SignInAsync(user, isPersistent: false, info.LoginProvider);
                    return LocalRedirect(returnUrl);
                }
            }

            foreach (var error in createResult.Errors)
            {
                ModelState.AddModelError(string.Empty, error.Description);
            }
            
            TempData["ErrorMessage"] = "Error creating user account.";
            return RedirectToAction(nameof(Login));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            // Sign out from the application
            await _signInManager.SignOutAsync();
            
            // Clear the external cookie to ensure Google account selection on next login
            await HttpContext.SignOutAsync(IdentityConstants.ExternalScheme);
            
            _logger.LogInformation("User logged out.");
            return RedirectToAction(nameof(Login));
        }

        [HttpGet]
        public IActionResult AccessDenied()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Lockout()
        {
            return View();
        }
    }
}