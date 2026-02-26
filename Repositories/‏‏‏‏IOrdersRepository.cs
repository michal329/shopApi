using DTOs;
using Entities;
using Entities.Models; 


namespace Repositories
{
    public interface IOrdersRepository
    {
        
        Task<Order?> GetOrderById(int id);

    
        Task<Order> AddOrder(Order order);

     
        Task<Order> UpdateOrder(Order order);

      

        
        Task<IEnumerable<Order>> GetAllOrders();

        Task UpdateOrderStatus(int orderId, string status, UserDTO user);
    }
}