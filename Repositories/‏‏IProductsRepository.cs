using Entities.Models;

namespace Repositories
{
    public interface IProductsRepository
    {
        Task<(List<Product> Items, int TotalCount)> GetProducts(
            int position, int skip, int?[] categoryIds,
            string? description, int? maxPrice, int? minPrice);

        Task<IEnumerable<Product>> GetProducts();

        Task<Product> AddProduct(Product product);
        Task<bool> UpdateProduct(int id, Product product);
        Task<bool> DeleteProduct(int id);
    }
}