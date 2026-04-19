using DTOs;

namespace Services
{
    public interface IProductsServices
    {
        Task<PageResponseDTO<ProductDTO>> GetProducts(
            int position, int skip, int?[] categoryIds,
            string? description, int? maxPrice, int? minPrice);

        Task<IEnumerable<ProductDTO>> GetProducts();

        Task<ProductDTO> AddProduct(ProductDTO product);
        Task<bool> UpdateProduct(int id, ProductDTO product);
        Task<bool> DeleteProduct(int id);
    }
}