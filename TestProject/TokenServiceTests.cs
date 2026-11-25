using System.Text;
using DTOs;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.JsonWebTokens;
using Microsoft.IdentityModel.Tokens;
using Services;

namespace TestProject
{
    public class TokenServiceTests
    {
        private const string Key = "test-signing-key-that-is-at-least-32-bytes-long!";
        private static readonly DateTimeOffset Now = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);

        private sealed class FixedTime : TimeProvider
        {
            public override DateTimeOffset GetUtcNow() => Now;
        }

        private static readonly JwtSettings Settings = new()
        {
            Issuer = "WebApiShop", Audience = "WebApiShop.Client", Key = Key, ExpiresMinutes = 60,
        };

        private static TokenService Service() => new(Options.Create(Settings), new FixedTime());

        private static UserDTO User(bool isAdmin) =>
            new(7, "dana@test.com", "Dana", "Levi", isAdmin, new List<OrderDTO>());

        private static async Task<TokenValidationResult> Validate(string token, string key = Key) =>
            await new JsonWebTokenHandler().ValidateTokenAsync(token, new TokenValidationParameters
            {
                ValidIssuer = Settings.Issuer,
                ValidAudience = Settings.Audience,
                IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key)),
                ValidateLifetime = false, // the token is issued at a fixed time in the past
            });

        [Theory]
        [InlineData(true, Roles.Admin)]
        [InlineData(false, Roles.Customer)]
        public async Task Token_CarriesTheUsersIdEmailAndRole(bool isAdmin, string expectedRole)
        {
            AccessToken token = Service().CreateToken(User(isAdmin));
            TokenValidationResult result = await Validate(token.Token);

            Assert.True(result.IsValid);
            Assert.Equal("7", result.Claims[ShopClaims.UserId]);
            Assert.Equal("dana@test.com", result.Claims[ShopClaims.Email]);
            Assert.Equal(expectedRole, result.Claims[ShopClaims.Role]);
        }

        [Fact]
        public void Token_ExpiresAfterTheConfiguredMinutes()
        {
            AccessToken token = Service().CreateToken(User(false));

            Assert.Equal(Now.UtcDateTime.AddMinutes(60), token.ExpiresAtUtc);
            Assert.Equal(Now.AddMinutes(60).ToUnixTimeSeconds(),
                new JsonWebToken(token.Token).GetPayloadValue<long>("exp"));
        }

        [Fact]
        public async Task Token_SignedWithAnotherKey_IsRejected()
        {
            AccessToken token = Service().CreateToken(User(true));

            TokenValidationResult result = await Validate(token.Token, "a-different-key-that-is-also-32-bytes-long!!");

            Assert.False(result.IsValid);
        }
    }
}
