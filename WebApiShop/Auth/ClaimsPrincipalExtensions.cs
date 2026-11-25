using System.Security.Claims;
using Services;

namespace WebApiShop.Auth
{
    public static class ClaimsPrincipalExtensions
    {
        /// <summary>The signed-in user's id, from the token's "sub" claim.</summary>
        public static int GetUserId(this ClaimsPrincipal user) =>
            int.Parse(user.FindFirstValue(ShopClaims.UserId)
                      ?? throw new InvalidOperationException("The token has no user id."));

        public static bool IsAdmin(this ClaimsPrincipal user) => user.IsInRole(Roles.Admin);

        /// <summary>True for the user themself, or for an admin.</summary>
        public static bool CanAccessUser(this ClaimsPrincipal user, int userId) =>
            user.IsAdmin() || user.GetUserId() == userId;
    }
}
