using Entities;
using DTOs;
namespace Services
{
    public interface IOrdersServices
    {
        Task<OrderDTO?> GetOrderById(int id);
        Task<OrderDTO> AddOrder(OrderDTO order);

        Task<IEnumerable<OrderDTO>> GetAllOrders();
        Task UpdateStatus(int orderId, string status, UserDTO user);
    }
}