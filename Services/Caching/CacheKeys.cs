namespace Services
{
    /// <summary>
    /// Redis key names. Every key starts with "shop:" so the API's keys are
    /// easy to find with KEYS shop:* in redis-cli.
    ///
    /// Product keys contain a version. Any product change writes a new
    /// version, so every cached product list (the full list and every
    /// filter/page combination) becomes unreachable at once, without having
    /// to find and delete each key. The old entries expire by their TTL.
    /// </summary>
    public static class CacheKeys
    {
        public const string Categories = "shop:categories:all";
        public const string ProductsVersion = "shop:products:version";

        public static string AllProducts(string version) => $"shop:products:v{version}:all";

        public static string ProductsPage(
            string version, int position, int skip, int?[]? categoryIds,
            string? description, int? maxPrice, int? minPrice)
        {
            // Sort the category ids so ?categoryIds=2&categoryIds=1 and
            // ?categoryIds=1&categoryIds=2 share one cache entry.
            string categories = categoryIds is { Length: > 0 }
                ? string.Join(",", categoryIds.Where(id => id.HasValue).Select(id => id!.Value).OrderBy(id => id))
                : "all";

            return $"shop:products:v{version}:page:pos={position}:size={skip}:cat={categories}" +
                   $":desc={description}:max={maxPrice}:min={minPrice}";
        }
    }
}
