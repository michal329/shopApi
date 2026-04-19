using AutoMapper;
using DTOs;
using Entities.Models;
using Microsoft.Extensions.Logging;
using Moq;
using Org.BouncyCastle.Crypto;
using Repositories;
using Services;

namespace TestProject
{
    public class OrderSumValidationTests
    {
        private readonly Mock<IOrdersRepository> _mockOrdersRepository;
        private readonly Mock<IProductsRepository> _mockProductsRepository;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<ILogger<OrdersServices>> _mockLogger;
        private readonly OrdersServices _ordersServices;

        public OrderSumValidationTests()
        {
            _mockOrdersRepository = new Mock<IOrdersRepository>();
            _mockProductsRepository = new Mock<IProductsRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockLogger = new Mock<ILogger<OrdersServices>>();

            _ordersServices = new OrdersServices(
                _mockOrdersRepository.Object,
                _mockProductsRepository.Object,
                _mockMapper.Object,
                _mockLogger.Object);
        }

        [Fact]
        public async Task ValidateOrderSum_ShouldReturnTrue_WhenOrderSumMatchesCalculatedSum()
        {
            // Arrange
            var products = new List<Product>
            {
                new Product { ProductId = 1, ProductName = "Laptop", Price = 1000m },
                new Product { ProductId = 2, ProductName = "Mouse",  Price = 50m   }
            };

            _mockProductsRepository
                .Setup(r => r.GetProducts())
                .ReturnsAsync(products);

            var orderItems = new List<OrderItemDTO>
            {
                new OrderItemDTO(OrderId: 1, ProductId: 1, Quantity: 2),  // 2 × 1000 = 2000
                new OrderItemDTO(OrderId: 1, ProductId: 2, Quantity: 3)   // 3 × 50  =  150
            };

            var order = new OrderDTO(
                OrderId: 1,
                OrderDate: DateOnly.FromDateTime(DateTime.Now),
                OrderSum: 2150,   // correct
                UserId: 1,
                Status: "Pending",
                OrderItems: orderItems
            );

            // Act
            var result = await _ordersServices.ValidateOrderSum(order);

            // Assert
            Assert.True(result);
        }

        [Fact]
        public async Task ValidateOrderSum_ShouldReturnFalse_WhenOrderSumDoesNotMatchCalculatedSum()
        {
            // Arrange
            var products = new List<Product>
            {
                new Product { ProductId = 1, ProductName = "Laptop", Price = 1000m },
                new Product { ProductId = 2, ProductName = "Mouse",  Price = 50m   }
            };

            _mockProductsRepository
                .Setup(r => r.GetProducts())
                .ReturnsAsync(products);

            var orderItems = new List<OrderItemDTO>
            {
                new OrderItemDTO(OrderId: 1, ProductId: 1, Quantity: 2),
                new OrderItemDTO(OrderId: 1, ProductId: 2, Quantity: 3)
            };

            var order = new OrderDTO(
                OrderId: 1,
                OrderDate: DateOnly.FromDateTime(DateTime.Now),
                OrderSum: 999,    // wrong — real sum is 2150
                UserId: 1,
                Status: "Pending",
                OrderItems: orderItems
            );

            // Act
            var result = await _ordersServices.ValidateOrderSum(order);

            // Assert
            Assert.False(result);
        }

        [Fact]
        public async Task ValidateOrderSum_ShouldReturnFalse_WhenOrderItemsAreEmpty()
        {
            var order = new OrderDTO(
                OrderId: 1,
                OrderDate: DateOnly.FromDateTime(DateTime.Now),
                OrderSum: 0,
                UserId: 1,
                Status: "Pending",
                OrderItems: new List<OrderItemDTO>()
            );

            var result = await _ordersServices.ValidateOrderSum(order);

            Assert.False(result);
        }
    }
}