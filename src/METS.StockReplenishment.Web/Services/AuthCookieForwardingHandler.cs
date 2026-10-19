namespace METS.StockReplenishment.Web.Services;

public sealed class AuthCookieForwardingHandler : DelegatingHandler
{
	private readonly IHttpContextAccessor _httpContextAccessor;

	public AuthCookieForwardingHandler(IHttpContextAccessor httpContextAccessor)
	{
		_httpContextAccessor = httpContextAccessor;
	}

	protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
	{
		var cookieHeader = _httpContextAccessor.HttpContext?.Request.Headers.Cookie.ToString();

		if (!string.IsNullOrWhiteSpace(cookieHeader))
		{
			request.Headers.Remove("Cookie");
			request.Headers.TryAddWithoutValidation("Cookie", cookieHeader);
		}

		return base.SendAsync(request, cancellationToken);
	}
}
