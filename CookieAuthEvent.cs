using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using HamsterWorld.Models;
using System.Security.Claims;

public class CustomCookieAuthenticationEvents : CookieAuthenticationEvents
{
	private ApplicationContext _context;
	public CustomCookieAuthenticationEvents(ApplicationContext context)
	{
		_context = context;
	}

	public override async Task ValidatePrincipal(CookieValidatePrincipalContext context)
	{
		//if refresh cookie and Auth Cookie are existing
		if(context.Request.Cookies.ContainsKey("RefreshCookie") && context.Request.Cookies.ContainsKey("MyCookie"))
		{
			return;
		}

		ClaimsPrincipal? authCookies = context.Principal;
		int? userId = GetUserIdFromCookies(authCookies);
		
		//If auth cookies has been corrupted
		if(userId == null)
		{
			await LogoutUser(context);
			return;
		}

		//if user's role has been changed by admin
		UserWithChangedRole? userFromBlacklist = await _context.Blacklist.FindAsync(userId);
		if(userFromBlacklist != null)
		{
			await LogoutUser(context);

			_context.Blacklist.Remove(userFromBlacklist!);
			await _context.SaveChangesAsync();
			return;
		}

		SendRefreshCookieToUser(context);
	}

	private int? GetUserIdFromCookies(ClaimsPrincipal? userPrincipal)
	{
		string? userIdString = userPrincipal?.Claims
										.Where(c => c.Type == ClaimTypes.NameIdentifier)
										.Select(c => c.Value)
										.FirstOrDefault();

		if(string.IsNullOrEmpty(userIdString))
		{
			return null;
		}

		int result;
		if(!int.TryParse(userIdString, out result))
		{
			return null;
		}

		return result;
	}

	private void SendRefreshCookieToUser(CookieValidatePrincipalContext context)
	{
		CookieOptions opt = new CookieOptions();
		opt.Expires = new DateTimeOffset(DateTime.UtcNow.AddHours(1));
		opt.IsEssential = true;
		opt.HttpOnly = true;
		opt.SameSite = SameSiteMode.Strict;
		context.Response.Cookies.Append("RefreshCookie", "", opt);
	}

	private async Task LogoutUser(CookieValidatePrincipalContext context)
	{
		context.Response.Cookies.Delete("RefreshCookie");
		context.RejectPrincipal();
		await context.HttpContext.SignOutAsync("Cookies");
	}
}