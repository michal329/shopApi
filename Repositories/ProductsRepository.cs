using Entities;
using Entities.Models;
using Microsoft.EntityFrameworkCore;

namespace Repositories
{
    public class ProductsRepository : IProductsRepository
    {
        private readonly ApiShopContext _context;

        public ProductsRepository(ApiShopContext context)
        {
            _context = context;
        }

        public async Task<(List<Product> Items, int TotalCount)> GetProducts(
            int position,
            int skip,
            int?[] categoryIds,
            string? description,
            int? maxPrice,
            int? minPrice)
        {
            var query = _context.Products
                .Where(product => product.IsActive == true)
                .Where(product =>
                    (description == null || product.Description.Contains(description)) &&
                    (maxPrice == null || product.Price <= (decimal)maxPrice) &&
                    (minPrice == null || product.Price >= (decimal)minPrice) &&
                    (categoryIds.Length == 0 || categoryIds.Contains(product.CategoryId))
                )
                .OrderBy(product => product.Price);

            List<Product> products = await query
                .Skip((position - 1) * skip)
                .Take(skip)
                .Include(product => product.Category)
                .ToListAsync();

            var total = await query.CountAsync();

            return (products, total);
        }

        public async Task<Product> AddProduct(Product product)
        {
            product.IsActive = true;
            await _context.Products.AddAsync(product);
            await _context.SaveChangesAsync();
            return product;
        }

        public async Task<bool> UpdateProduct(int id, Product product)
        {
            var existing = await _context.Products.FindAsync(id);
            if (existing == null) return false;

            existing.ProductName = product.ProductName;
            existing.Price = product.Price;
            existing.CategoryId = product.CategoryId;
            existing.Description = product.Description;
            existing.ImageUrl = product.ImageUrl;
            existing.Quantity = product.Quantity;

            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteProduct(int id)
        {
            var product = await _context.Products
                .Include(p => p.OrderItems)
                    .ThenInclude(oi => oi.Order)
                .FirstOrDefaultAsync(p => p.ProductId == id);

            if (product == null) return false;

            // Check if product is in any undelivered order
            bool hasUndeliveredOrders = product.OrderItems
                .Any(oi => oi.Order != null && oi.Order.Status != "Delivered");

            if (hasUndeliveredOrders)
                throw new InvalidOperationException("Cannot delete a product that is part of an undelivered order.");

            // Soft delete — keeps history intact
            product.IsActive = false;
            await _context.SaveChangesAsync();
            return true;
        }
    }
}