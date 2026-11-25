using Microsoft.Extensions.Options;
using Services;

namespace WebApiShop.Auth
{
    /// <summary>
    /// Puts the access token in an HTTP-only cookie: JavaScript in the browser
    /// can't read it (so an XSS bug can't steal it), and the browser sends it
    /// on every request to the API. SameSite=Strict keeps other sites from
    /// sending it (CSRF protection).
    /// </summary>
    public class AuthCookie
    {
        private readonly JwtSettings _settings;

        public AuthCookie(IOptions<JwtSettings> settings)
        {
            _settings = settings.Value;
        }

        public void Append(HttpResponse response, AccessToken token)
        {
            response.Cookies.Append(_settings.CookieName, token.Token, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/",
                Expires = token.ExpiresAtUtc,
            });
        }

        public void Delete(HttpResponse response)
        {
            response.Cookies.Delete(_settings.CookieName, new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                SameSite = SameSiteMode.Strict,
                Path = "/",
            });
        }
    }
}
