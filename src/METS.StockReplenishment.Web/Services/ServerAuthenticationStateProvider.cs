using Microsoft.AspNetCore.Components.Authorization;
using System.Security.Claims;

namespace METS.StockReplenishment.Web.Services;

public class ServerAuthenticationStateProvider : AuthenticationStateProvider
{
	private readonly IHttpContextAccessor _httpContextAccessor;

	public ServerAuthenticationStateProvider(IHttpContextAccessor httpContextAccessor)
	{
		_httpContextAccessor = httpContextAccessor;
	}

	public override Task<AuthenticationState> GetAuthenticationStateAsync()
	{
		var httpContext = _httpContextAccessor.HttpContext;

		if (httpContext?.User?.Identity?.IsAuthenticated ?? false)
		{
			return Task.FromResult(new AuthenticationState(httpContext.User));
		}

		var anonymous = new ClaimsPrincipal(new ClaimsIdentity());
		return Task.FromResult(new AuthenticationState(anonymous));
	}
}
