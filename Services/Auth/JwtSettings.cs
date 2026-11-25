namespace Services
{
    /// <summary>
    /// JWT settings, bound from the "Jwt" section of appsettings.json.
    /// The signing key is not in appsettings.json: it comes from
    /// appsettings.Development.json locally, or from the Jwt__Key
    /// environment variable anywhere else.
    /// </summary>
    public class JwtSettings
    {
        public const string SectionName = "Jwt";

        public string Issuer { get; set; } = "";
        public string Audience { get; set; } = "";
        public string Key { get; set; } = "";
        public int ExpiresMinutes { get; set; } = 60;
        public string CookieName { get; set; } = "shop_access_token";
    }
}
