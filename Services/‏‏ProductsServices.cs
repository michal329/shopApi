using Entities.Models;
using Repositories;
using DTOs;
using AutoMapper;

namespace Services
{
    public class ProductsServices : IProductsServices
    {
        private readonly IProductsRepository _repository;
        private readonly IMapper _mapper;

        public ProductsServices(IProductsRepository repository, IMapper mapper)
        {
            _repository = repository;
            _mapper = mapper;
        }

        public async Task<PageResponseDTO<ProductDTO>> GetProducts(
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

        public async Task<IEnumerable<ProductDTO>> GetProducts()
        {
            return _mapper.Map<IEnumerable<Product>, IEnumerable<ProductDTO>>(
                await _repository.GetProducts());
        }

        public async Task<ProductDTO> AddProduct(ProductDTO product)
        {
            Product entity = _mapper.Map<Product>(product);
            Product created = await _repository.AddProduct(entity);
            return _mapper.Map<ProductDTO>(created);
        }

        public async Task<bool> UpdateProduct(int id, ProductDTO product)
        {
            Product entity = _mapper.Map<Product>(product);
            return await _repository.UpdateProduct(id, entity);
        }

        public async Task<bool> DeleteProduct(int id)
        {
            return await _repository.DeleteProduct(id);
        }
    }
}