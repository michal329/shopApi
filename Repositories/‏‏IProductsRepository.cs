using Entities.Models;

namespace Repositories
{
    public interface IProductsRepository
    {
        public Task<(List<Product> Items, int TotalCount)> GetProducts(int position, int skip, int?[] categoryIds,
         string? description, int? maxPrice, int? minPrice);

        public Task<Product> AddProduct(Product product);
        public Task<bool> UpdateProduct(int id, Product product);
        public Task<bool> DeleteProduct(int id);
    }
}