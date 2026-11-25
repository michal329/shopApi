using AutoMapper;
using DTOs;
using Entities.Models;
using Microsoft.Extensions.Caching.Distributed;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using Moq;
using Repositories;
using Services;

namespace TestProject
{
    /// <summary>
    /// Cache-aside behavior of the product and category services, against an
    /// in-memory IDistributedCache (the same interface Redis implements).
    /// </summary>
    public class CachingTests
    {
        private readonly IMapper _mapper =
            new MapperConfiguration(cfg => cfg.AddProfile<global::WebApiShop.AutoMapper>()).CreateMapper();

        private readonly IOptions<CacheSettings> _settings =
            Options.Create(new CacheSettings { ProductsTtlSeconds = 60, CategoriesTtlSeconds = 300 });

        private static ICacheService MemoryCache() => new DistributedCacheService(
            new MemoryDistributedCache(Options.Create(new MemoryDistributedCacheOptions())),
            NullLogger<DistributedCacheService>.Instance);

        private static List<Product> SomeProducts() => new()
        {
            new Product { ProductId = 1, ProductName = "Laptop", Price = 1000m, CategoryId = 1, Description = "x", IsActive = true },
            new Product { ProductId = 2, ProductName = "Mouse", Price = 50m, CategoryId = 1, Description = "y", IsActive = true },
        };

        private ProductsServices ProductsService(Mock<IProductsRepository> repo, ICacheService cache) =>
            new(repo.Object, _mapper, cache, _settings);

        [Fact]
        public async Task GetProducts_SecondCall_IsServedFromCache()
        {
            var repo = new Mock<IProductsRepository>();
            repo.Setup(r => r.GetProducts()).ReturnsAsync(SomeProducts());
            var service = ProductsService(repo, MemoryCache());

            var first = await service.GetProducts();
            var second = await service.GetProducts();

            Assert.Equal(2, first.Count());
            Assert.Equal(first.Select(p => p.ProductName), second.Select(p => p.ProductName));
            repo.Verify(r => r.GetProducts(), Times.Once);
        }

        [Theory]
        [InlineData("add")]
        [InlineData("update")]
        [InlineData("delete")]
        public async Task ChangingAProduct_InvalidatesCachedProductLists(string change)
        {
            var repo = new Mock<IProductsRepository>();
            repo.Setup(r => r.GetProducts()).ReturnsAsync(SomeProducts());
            repo.Setup(r => r.AddProduct(It.IsAny<Product>())).ReturnsAsync((Product p) => p);
            repo.Setup(r => r.UpdateProduct(It.IsAny<int>(), It.IsAny<Product>())).ReturnsAsync(true);
            repo.Setup(r => r.DeleteProduct(It.IsAny<int>())).ReturnsAsync(true);
            var service = ProductsService(repo, MemoryCache());
            var product = new ProductDTO(1, "Laptop", 900m, 1, "x", 5, null, true);

            await service.GetProducts();
            if (change == "add") await service.AddProduct(product);
            if (change == "update") await service.UpdateProduct(1, product);
            if (change == "delete") await service.DeleteProduct(1);
            await service.GetProducts();

            repo.Verify(r => r.GetProducts(), Times.Exactly(2));
        }

        [Fact]
        public async Task FailedUpdate_DoesNotInvalidateTheCache()
        {
            var repo = new Mock<IProductsRepository>();
            repo.Setup(r => r.GetProducts()).ReturnsAsync(SomeProducts());
            repo.Setup(r => r.UpdateProduct(It.IsAny<int>(), It.IsAny<Product>())).ReturnsAsync(false);
            var service = ProductsService(repo, MemoryCache());

            await service.GetProducts();
            await service.UpdateProduct(99, new ProductDTO(99, "Nope", 1m, 1, "x", 1, null, true));
            await service.GetProducts();

            repo.Verify(r => r.GetProducts(), Times.Once);
        }

        [Fact]
        public async Task FilteredPages_ShareAnEntry_WhenCategoryOrderDiffers_AndDifferOtherwise()
        {
            var repo = new Mock<IProductsRepository>();
            repo.Setup(r => r.GetProducts(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?[]>(),
                    It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int?>()))
                .ReturnsAsync((SomeProducts(), 2));
            var service = ProductsService(repo, MemoryCache());

            await service.GetProducts(1, 10, new int?[] { 1, 2 }, null, null, null);
            await service.GetProducts(1, 10, new int?[] { 2, 1 }, null, null, null); // same filter
            await service.GetProducts(2, 10, new int?[] { 1, 2 }, null, null, null); // another page

            repo.Verify(r => r.GetProducts(It.IsAny<int>(), It.IsAny<int>(), It.IsAny<int?[]>(),
                It.IsAny<string?>(), It.IsAny<int?>(), It.IsAny<int?>()), Times.Exactly(2));
        }

        [Fact]
        public async Task GetCategories_SecondCall_IsServedFromCache()
        {
            var repo = new Mock<ICategoriesRepository>();
            repo.Setup(r => r.GetCategories()).ReturnsAsync(new List<Category>
            {
                new Category { CategoryId = 1, CategoryName = "Computers" },
            });
            var service = new CategoriesServices(repo.Object, _mapper, MemoryCache(), _settings);

            await service.GetCategories();
            var second = await service.GetCategories();

            Assert.Equal("Computers", second.Single().CategoryName);
            repo.Verify(r => r.GetCategories(), Times.Once);
        }

        private sealed class ManualClock : TimeProvider
        {
            public DateTimeOffset Now { get; set; } = new(2026, 1, 1, 12, 0, 0, TimeSpan.Zero);
            public override DateTimeOffset GetUtcNow() => Now;
        }

        [Fact]
        public async Task AfterACacheFailure_TheCacheIsSkippedUntilTheBreakEnds()
        {
            var brokenRedis = new Mock<IDistributedCache>();
            brokenRedis.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Redis is unavailable"));
            var clock = new ManualClock();
            var cache = new DistributedCacheService(brokenRedis.Object, NullLogger<DistributedCacheService>.Instance,
                clock, TimeSpan.FromSeconds(30));

            await cache.GetOrCreateAsync("k", () => Task.FromResult(1), TimeSpan.FromMinutes(1)); // fails, opens the break
            await cache.GetOrCreateAsync("k", () => Task.FromResult(1), TimeSpan.FromMinutes(1)); // skips Redis
            brokenRedis.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Once);

            clock.Now += TimeSpan.FromSeconds(31);
            int value = await cache.GetOrCreateAsync("k", () => Task.FromResult(1), TimeSpan.FromMinutes(1)); // tries again
            brokenRedis.Verify(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()), Times.Exactly(2));
            Assert.Equal(1, value);
        }

        [Fact]
        public async Task WhenTheCacheIsDown_DataStillComesFromTheDatabase()
        {
            var brokenRedis = new Mock<IDistributedCache>();
            brokenRedis.Setup(c => c.GetAsync(It.IsAny<string>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Redis is unavailable"));
            brokenRedis.Setup(c => c.SetAsync(It.IsAny<string>(), It.IsAny<byte[]>(),
                    It.IsAny<DistributedCacheEntryOptions>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Redis is unavailable"));
            var cache = new DistributedCacheService(brokenRedis.Object, NullLogger<DistributedCacheService>.Instance);

            var repo = new Mock<IProductsRepository>();
            repo.Setup(r => r.GetProducts()).ReturnsAsync(SomeProducts());
            var service = ProductsService(repo, cache);

            var products = await service.GetProducts();

            Assert.Equal(2, products.Count());
        }
    }
}
