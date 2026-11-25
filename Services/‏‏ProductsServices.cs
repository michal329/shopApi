using Entities.Models;
using Repositories;
using DTOs;
using AutoMapper;
using Microsoft.Extensions.Options;

namespace Services
{
    public class ProductsServices : IProductsServices
    {
        private readonly IProductsRepository _repository;
        private readonly IMapper _mapper;
        private readonly ICacheService _cache;
        private readonly TimeSpan _ttl;

        public ProductsServices(IProductsRepository repository, IMapper mapper,
            ICacheService cache, IOptions<CacheSettings> cacheSettings)
        {
            _repository = repository;
            _mapper = mapper;
            _cache = cache;
            _ttl = TimeSpan.FromSeconds(cacheSettings.Value.ProductsTtlSeconds);
        }

        public async Task<PageResponseDTO<ProductDTO>> GetProducts(
            int position, int skip, int?[] categoryIds,
            string? description, int? maxPrice, int? minPrice)
        {
            string key = CacheKeys.ProductsPage(await GetProductsVersion(),
                position, skip, categoryIds, description, maxPrice, minPrice);

            return await _cache.GetOrCreateAsync(key,
                () => LoadProductsPage(position, skip, categoryIds, description, maxPrice, minPrice), _ttl);
        }

        public async Task<IEnumerable<ProductDTO>> GetProducts()
        {
            string key = CacheKeys.AllProducts(await GetProductsVersion());
            return await _cache.GetOrCreateAsync(key, async () =>
                _mapper.Map<List<Product>, List<ProductDTO>>((await _repository.GetProducts()).ToList()), _ttl);
        }

        public async Task<ProductDTO> AddProduct(ProductDTO product)
        {
            Product entity = _mapper.Map<Product>(product);
            Product created = await _repository.AddProduct(entity);
            await InvalidateProducts();
            return _mapper.Map<ProductDTO>(created);
        }

        public async Task<bool> UpdateProduct(int id, ProductDTO product)
        {
            Product entity = _mapper.Map<Product>(product);
            bool updated = await _repository.UpdateProduct(id, entity);
            if (updated)
                await InvalidateProducts();
            return updated;
        }

        public async Task<bool> DeleteProduct(int id)
        {
            bool deleted = await _repository.DeleteProduct(id);
            if (deleted)
                await InvalidateProducts();
            return deleted;
        }

        private async Task<PageResponseDTO<ProductDTO>> LoadProductsPage(
            int position, int skip, int?[] categoryIds,
            string? description, int? maxPrice, int? minPrice)
        {
            (List<Product>, int) response = await _repository.GetProducts(
                position, skip, categoryIds, description, maxPrice, minPrice);

            List<ProductDTO> data = _mapper.Map<List<Product>, List<ProductDTO>>(response.Item1);

            PageResponseDTO<ProductDTO> pageResponse = new();
            pageResponse.Data = data;
            pageResponse.TotalItems = response.Item2;
            pageResponse.CurrentPage = position;
            pageResponse.PageSize = skip;
            pageResponse.HasPreviousPage = position > 1;

            int numOfPages = skip > 0 ? pageResponse.TotalItems / skip : 0;
            if (skip > 0 && pageResponse.TotalItems % skip != 0)
                numOfPages++;
            pageResponse.HasNextPage = position < numOfPages;

            return pageResponse;
        }

        // "0" until the first product change (or if the cache is unavailable).
        private async Task<string> GetProductsVersion() =>
            await _cache.GetStringAsync(CacheKeys.ProductsVersion) ?? "0";

        // A new version makes every cached product list unreachable at once.
        private Task InvalidateProducts() =>
            _cache.SetStringAsync(CacheKeys.ProductsVersion, Guid.NewGuid().ToString("N"));
    }
}
