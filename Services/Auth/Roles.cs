namespace Services
{
    public static class Roles
    {
        public const string Admin = "Admin";
        public const string Customer = "Customer";
    }

    /// <summary>Claim names used in the JWT.</summary>
    public static class ShopClaims
    {
        public const string UserId = "sub";
        public const string Email = "email";
        public const string Name = "name";
        public const string Role = "role";
    }
}
