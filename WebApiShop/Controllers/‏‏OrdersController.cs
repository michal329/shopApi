using Microsoft.AspNetCore.Mvc;
using Services;
using DTOs;
using WebApiShop.Auth;

namespace WebApiShop.Controllers
{
    // Every endpoint here needs a signed-in user (the app's default policy).
    [Route("api/[controller]")]
    [ApiController]
    public class OrdersController : ControllerBase
    {
        private readonly IOrdersServices _ordersServices;
        private readonly IUserServices _userServices;

        public OrdersController(IOrdersServices ordersServices, IUserServices userServices)
        {
            _ordersServices = ordersServices;
            _userServices = userServices;
        }

        // A customer can see their own orders; an admin can see any order.
        [HttpGet("{id}")]
        public async Task<ActionResult<OrderDTO>> Get(int id)
        {
            var order = await _ordersServices.GetOrderById(id);
            if (order == null) return NotFound();
            if (!User.CanAccessUser(order.UserId)) return Forbid();
            return Ok(order);
        }

        [HttpGet]
        [AuthorizeRoles(Roles.Admin)]
        public async Task<ActionResult<IEnumerable<OrderDTO>>> GetAll()
        {
            var orders = await _ordersServices.GetAllOrders();
            return Ok(orders);
        }

        // The order always belongs to the signed-in user, whatever UserId the body says.
        [HttpPost]
        public async Task<ActionResult<OrderDTO>> Post([FromBody] OrderDTO newOrder)
        {
            var createdOrder = await _ordersServices.AddOrder(newOrder with { UserId = User.GetUserId() });
            if (createdOrder == null) return BadRequest();
            return CreatedAtAction(nameof(Get), new { id = createdOrder.OrderId }, createdOrder);
        }

        // Admins move an order through its statuses; the customer confirms
        // delivery (the rules are in OrdersServices.UpdateStatus). The user
        // comes from the token, not from the request, so it can't be faked.
        [HttpPut("{id}/status")]
        public async Task<IActionResult> UpdateStatus(int id, [FromBody] string status)
        {
            try
            {
                var user = await _userServices.GetUserById(User.GetUserId());
                if (user == null) return Unauthorized();

                await _ordersServices.UpdateStatus(id, status, user);
                return NoContent();
            }
            catch (UnauthorizedAccessException ex)
            {
                return StatusCode(StatusCodes.Status403Forbidden, ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(ex.Message);
            }
        }
    }
}
