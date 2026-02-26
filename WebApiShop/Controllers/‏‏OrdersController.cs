using Microsoft.AspNetCore.Mvc;
using Services;
using DTOs;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebApiShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IOrdersServices _ordersServices;
        private readonly IUserServices _userServices; // לבדיקת IsAdmin

        public OrdersController(IOrdersServices ordersServices, IUserServices userServices)
        {
            _ordersServices = ordersServices;
            _userServices = userServices;
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDTO>> Get(int id)
        {
            var order = await _ordersServices.GetOrderById(id);
            if (order != null) return Ok(order);
            return NotFound();
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<OrderDTO>>> GetAll()
        {
            // שליפת כל ההזמנות (לצורך תצוגת מנהל)
            var orders = await _ordersServices.GetAllOrders();
            return Ok(orders);
        }

        [HttpPost]
        public async Task<ActionResult<OrderDTO>> Post([FromBody] OrderDTO newOrder)
        {
            // יצירת הזמנה חדשה
            var createdOrder = await _ordersServices.AddOrder(newOrder);
            if (createdOrder == null) return BadRequest();
            return CreatedAtAction(nameof(Get), new { id = createdOrder.OrderId }, createdOrder);
        }

        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status, [FromQuery] int userId)
        {
            try
            {
               
                var user = await _userServices.GetUserById(userId);
                if (user == null) return Unauthorized();

                // קריאה לסרביס - הוא זה שמנהל את ה"מותר" ו"אסור"
                await _ordersServices.UpdateStatus(id, status, user);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return Forbid(ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}