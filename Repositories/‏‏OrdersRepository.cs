using DTOs;
using Entities; // ודאי שה-Namespace תואם לתיקיית ה-Entities שלך
using Entities.Models;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Repositories
{
    public class OrdersRepository : IOrdersRepository
    {
        private readonly ApiShopContext _context;

        public OrdersRepository(ApiShopContext context)
        {
            _context = context;
        }

   
        public async Task<Order?> GetOrderById(int id)
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .FirstOrDefaultAsync(o => o.OrderId == id);
        }


        public async Task<Order> AddOrder(Order order)
        {
            await _context.Orders.AddAsync(order);
            await _context.SaveChangesAsync();
            return order;
        }

    
        public async Task<IEnumerable<Order>> GetAllOrders()
        {
            return await _context.Orders
                .Include(o => o.OrderItems)
                .ToListAsync();
        }


        public async Task<Order> UpdateOrder(Order order)
        {
            
            _context.Orders.Update(order);
            await _context.SaveChangesAsync();
            return order;
        }

        public async Task UpdateOrderStatus(int orderId, string newStatus, UserDTO user)
        {
            var order = await _context.Orders.FindAsync(orderId);
            if (order != null)
            {
                order.Status = newStatus;
                await _context.SaveChangesAsync();
            }
        }
    }
}