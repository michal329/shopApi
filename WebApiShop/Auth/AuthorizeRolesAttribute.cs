using Microsoft.AspNetCore.Authorization;

namespace WebApiShop.Auth
{
    /// <summary>
    /// Requires a signed-in user with one of the given roles, for example
    /// [AuthorizeRoles(Roles.Admin)]. Without a valid token the response is
    /// 401; with a token for another role it is 403.
    /// </summary>
    [AttributeUsage(AttributeTargets.Class | AttributeTargets.Method, AllowMultiple = true)]
    public class AuthorizeRolesAttribute : AuthorizeAttribute
    {
        public AuthorizeRolesAttribute(params string[] roles)
        {
            Roles = string.Join(",", roles);
        }
    }
}
