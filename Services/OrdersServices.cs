using DTOs;
using AutoMapper;
using Repositories;
using Entities.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Services
{
    public class OrdersServices : IOrdersServices
    {
        private readonly IOrdersRepository _orders;
        private readonly IMapper _mapper;

        public OrdersServices(IOrdersRepository orders, IMapper mapper)
        {
            _orders = orders;
            _mapper = mapper;
        }

        public async Task<OrderDTO?> GetOrderById(int id)
        {
            var order = await _orders.GetOrderById(id);
            if (order == null) return null;
            return _mapper.Map<Order, OrderDTO>(order);
        }

        public async Task<OrderDTO> AddOrder(OrderDTO order)
        {
            var orderEntity = _mapper.Map<OrderDTO, Order>(order);

        
            orderEntity.Status = "Pending";

            var newOrder = await _orders.AddOrder(orderEntity);
            return _mapper.Map<Order, OrderDTO>(newOrder);
        }

        public async Task<IEnumerable<OrderDTO>> GetAllOrders()
        {
            var orders = await _orders.GetAllOrders();
            return _mapper.Map<IEnumerable<Order>, IEnumerable<OrderDTO>>(orders);
        }

        public async Task UpdateStatus(int orderId, string status,UserDTO user)
        {
       
            if (user.IsAdmin)
            {
                if (status == "Delivered")
                    throw new InvalidOperationException("רק הלקוח יכול לאשר שההזמנה הגיעה.");

                await _orders.UpdateOrderStatus(orderId, status, user);
                return;
            }

            if (status == "Delivered")
            {
                var order = await _orders.GetOrderById(orderId);
                if (order == null || order.UserId != user.UserId)
                    throw new UnauthorizedAccessException("אין הרשאה לעדכן הזמנה זו.");

                await _orders.UpdateOrderStatus(orderId, status, user);
                return;
            }

            throw new UnauthorizedAccessException("פעולה לא מורשית עבור סוג משתמש זה.");
        }
    }
}