using Entities.Models;
using Repositories;
using DTOs;
using AutoMapper;
using Microsoft.Extensions.Options;

namespace Services
{
    public class CategoriesServices : ICategoriesServices
    {
        private readonly ICategoriesRepository _categoriesRepository;
        private readonly IMapper _mapper;
        private readonly ICacheService _cache;
        private readonly TimeSpan _ttl;

        public CategoriesServices(ICategoriesRepository categoriesRepository, IMapper mapper,
            ICacheService cache, IOptions<CacheSettings> cacheSettings)
        {
            _categoriesRepository = categoriesRepository;
            _mapper = mapper;
            _cache = cache;
            _ttl = TimeSpan.FromSeconds(cacheSettings.Value.CategoriesTtlSeconds);
        }

        // Categories can't be changed through the API, so the TTL alone keeps them fresh.
        public async Task<IEnumerable<CategoryDTO>> GetCategories()
        {
            return await _cache.GetOrCreateAsync(CacheKeys.Categories, async () =>
                _mapper.Map<List<Category>, List<CategoryDTO>>((await _categoriesRepository.GetCategories()).ToList()), _ttl);
        }
    }
}
