using DTOs;
using AutoMapper;
using Repositories;
using Entities.Models;
using Microsoft.Extensions.Logging;

namespace Services
{
    public class OrdersServices : IOrdersServices
    {
        private readonly IOrdersRepository _orders;
        private readonly IProductsRepository _products;
        private readonly IMapper _mapper;
        private readonly ILogger<OrdersServices> _logger;

        public OrdersServices(
            IOrdersRepository orders,
            IProductsRepository products,
            IMapper mapper,
            ILogger<OrdersServices> logger)
        {
            _orders = orders;
            _products = products;
            _mapper = mapper;
            _logger = logger;
        }

        public async Task<OrderDTO?> GetOrderById(int id)
        {
            var order = await _orders.GetOrderById(id);
            if (order == null) return null;
            return _mapper.Map<Order, OrderDTO>(order);
        }

        public async Task<OrderDTO?> AddOrder(OrderDTO order)
        {
            if (order.OrderItems == null || !order.OrderItems.Any())
            {
                _logger.LogWarning($"Order rejected - No order items provided. UserId: {order.UserId}");
                return null;
            }

            var allProducts = await _products.GetProducts();
            double calculatedSum = 0;

            foreach (var item in order.OrderItems)
            {
                var product = allProducts.FirstOrDefault(p => p.ProductId == item.ProductId);
                if (product == null)
                {
                    _logger.LogWarning($"Order rejected - Product not found. ProductId: {item.ProductId}, UserId: {order.UserId}");
                    return null;
                }
                calculatedSum += (double)((double)product.Price * item.Quantity);
            }

            // Rebuild the DTO with the server-calculated sum (never trust client sum)
            var orderWithCalculatedSum = new OrderDTO(
                order.OrderId,
                order.OrderDate,
                calculatedSum,
                order.UserId,
                order.Status,
                order.OrderItems
            );

            Order placedOrder = await _orders.AddOrder(_mapper.Map<OrderDTO, Order>(orderWithCalculatedSum));
            _logger.LogInformation($"Order Id {placedOrder.OrderId} placed successfully. Sum: {calculatedSum}");

            return _mapper.Map<Order, OrderDTO>(placedOrder);
        }

        public async Task<IEnumerable<OrderDTO>> GetAllOrders()
        {
            var orders = await _orders.GetAllOrders();
            return _mapper.Map<IEnumerable<Order>, IEnumerable<OrderDTO>>(orders);
        }

        public async Task UpdateStatus(int orderId, string status, UserDTO user)
        {
            if (user.IsAdmin)
            {
                if (status == "Delivered")
                    throw new InvalidOperationException("Only the customer can confirm delivery.");

                await _orders.UpdateOrderStatus(orderId, status, user);
                return;
            }

            if (status == "Delivered")
            {
                var order = await _orders.GetOrderById(orderId);
                if (order == null || order.UserId != user.UserId)
                    throw new UnauthorizedAccessException("You are not authorized to update this order.");

                await _orders.UpdateOrderStatus(orderId, status, user);
                return;
            }

            throw new UnauthorizedAccessException("This action is not permitted for your user type.");
        }

        public async Task<bool> ValidateOrderSum(OrderDTO order)
        {
            if (order.OrderItems == null || !order.OrderItems.Any())
                return false;

            var allProducts = await _products.GetProducts();
            double calculatedSum = 0;

            foreach (var item in order.OrderItems)
            {
                var product = allProducts.FirstOrDefault(p => p.ProductId == item.ProductId);
                if (product == null)
                    return false;

                calculatedSum += (double)((double)product.Price * item.Quantity);
            }

            return Math.Abs(calculatedSum - order.OrderSum) < 0.01;
        }
    }
}