namespace Services
{
    /// <summary>Cache time-to-live values, bound from the "Cache" section of appsettings.json.</summary>
    public class CacheSettings
    {
        public const string SectionName = "Cache";

        public int ProductsTtlSeconds { get; set; } = 60;
        public int CategoriesTtlSeconds { get; set; } = 300;
    }
}
