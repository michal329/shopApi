using System.Security.Claims;
using System.Text;
using DTOs;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;

namespace Services
{
    public record AccessToken(string Token, DateTime ExpiresAtUtc);

    public interface ITokenService
    {
        AccessToken CreateToken(UserDTO user);
    }

    public class TokenService : ITokenService
    {
        private readonly JwtSettings _settings;
        private readonly TimeProvider _time;

        public TokenService(IOptions<JwtSettings> settings, TimeProvider time)
        {
            _settings = settings.Value;
            _time = time;
        }

        public AccessToken CreateToken(UserDTO user)
        {
            DateTime now = _time.GetUtcNow().UtcDateTime;
            DateTime expires = now.AddMinutes(_settings.ExpiresMinutes);

            var claims = new List<Claim>
            {
                new(ShopClaims.UserId, user.UserId.ToString()),
                new(ShopClaims.Email, user.Email),
                new(ShopClaims.Name, $"{user.FirstName} {user.LastName}".Trim()),
                new(ShopClaims.Role, user.IsAdmin ? Roles.Admin : Roles.Customer),
            };

            var descriptor = new SecurityTokenDescriptor
            {
                Issuer = _settings.Issuer,
                Audience = _settings.Audience,
                Subject = new ClaimsIdentity(claims),
                IssuedAt = now,
                NotBefore = now,
                Expires = expires,
                SigningCredentials = new SigningCredentials(
                    new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_settings.Key)),
                    SecurityAlgorithms.HmacSha256),
            };

            return new AccessToken(new JsonWebTokenHandler().CreateToken(descriptor), expires);
        }
    }
}
