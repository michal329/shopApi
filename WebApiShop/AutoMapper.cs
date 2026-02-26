using AutoMapper;
using DTOs;
using Entities.Models;

namespace WebApiShop
{
    public class AutoMapper : Profile
    {
        public AutoMapper()
        {
            CreateMap<Order, OrderDTO>().ReverseMap();
            CreateMap<OrderItem, OrderItemDTO>().ReverseMap();
            CreateMap<Category, CategoryDTO>().ReverseMap();
            CreateMap<User, UserDTO>().ReverseMap();
            CreateMap<User, ExisitingUserDTO>().ReverseMap();
            CreateMap<Product, ProductDTO>()
                .ForMember(dest => dest.ImageUrl, opt => opt.MapFrom(src => src.ImageUrl))
                .ForMember(dest => dest.IsActive, opt => opt.MapFrom(src => src.IsActive))
                .ReverseMap();
        }
    }
}