namespace WebApiShop.Auth
{
    public static class RateLimitPolicies
    {
        /// <summary>Tight per-IP limit for login and registration (see RateLimiting:Auth).</summary>
        public const string Auth = "auth";
    }
}
