using METS.StockReplenishment.Web.ViewModels.Account;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace METS.StockReplenishment.Web.Controllers;


/// <summary>
/// Handles account operations such as authenticating users, signing them in and out, and redirecting to the login page.
/// </summary>
/// <remarks>Mapped to the 'Account' route and excluded from API explorer. Uses UserManager and SignInManager for
/// credential validation and sign-in; issues the application authentication cookie on successful login and uses
/// LocalRedirect to prevent open-redirect attacks. The POST login and logout endpoints bypass antiforgery token
/// validation.</remarks>
[Route("Account")]
[ApiExplorerSettings(IgnoreApi = true)]
public class AccountController : Controller
{
	private readonly UserManager<IdentityUser> _userManager;
	private readonly SignInManager<IdentityUser> _signInManager;

	public AccountController(UserManager<IdentityUser> userManager, SignInManager<IdentityUser> signInManager)
	{
		_userManager = userManager;
		_signInManager = signInManager;
	}

	/// <summary>
	/// Authenticates the specified credentials, signs the user in, and redirects to the provided return URL or the
	/// requests page.
	/// </summary>
	/// <remarks>Validates ModelState, locates the user, checks the password using SignInManager, issues the sign-in
	/// cookie when successful, and uses LocalRedirect to prevent open-redirect attacks.</remarks>
	/// <param name="model">A LoginViewModel containing the username and password submitted from the login form.</param>
	/// <param name="returnUrl">An optional local URL to redirect to after successful sign-in; defaults to "/requests" when not provided.</param>
	/// <returns>An IActionResult that redirects to the login page on validation or authentication failure, or redirects to the
	/// return URL (or "/requests") on success.</returns>

	[HttpPost("login")]
	[IgnoreAntiforgeryToken]
	public async Task<IActionResult> Login([FromForm] LoginViewModel model, string? returnUrl = null)
	{
		if (!ModelState.IsValid)
		{
			return RedirectToAction("LoginPage", new { error = "Invalid credentials" });
		}

		var user = await _userManager.FindByNameAsync(model.Username);
		if (user == null)
		{
			return RedirectToAction("LoginPage", new { error = "Invalid username or password" });
		}

		var result = await _signInManager.CheckPasswordSignInAsync(user, model.Password, false);
		if (!result.Succeeded)
		{
			return RedirectToAction("LoginPage", new { error = "Invalid username or password" });
		}

		await _signInManager.SignInAsync(user, isPersistent: false);

		return LocalRedirect(returnUrl ?? "/requests");
	}


	/// <summary>
	/// Signs the current user out of the application authentication scheme and redirects to the LoginPage action.
	/// </summary>
	/// <remarks>Handles POST requests to 'logout' and bypasses antiforgery token validation.</remarks>
	/// <returns>An IActionResult that redirects the client to the LoginPage action.</returns>
	[HttpPost("logout")]
	[IgnoreAntiforgeryToken]
	public async Task<IActionResult> Logout()
	{
		await HttpContext.SignOutAsync(IdentityConstants.ApplicationScheme);
		return RedirectToAction("LoginPage");
	}

	/// <summary>
	/// Redirects to the login page, including an escaped error query parameter when provided.
	/// </summary>
	/// <remarks>The error value is URL-escaped using Uri.EscapeDataString before being appended to the query
	/// string.</remarks>
	/// <param name="error">Optional error message to include in the query string; when null or whitespace, the redirect omits the error
	/// parameter.</param>
	/// <returns>A redirect result to the login page; includes an escaped error query parameter when an error is provided.</returns>
	[HttpGet("login")]
	public IActionResult LoginPage(string? error = null)
	{
		if (string.IsNullOrWhiteSpace(error))
		{
			return Redirect("/login");
		}

		return Redirect($"/login?error={Uri.EscapeDataString(error)}");
	}
}