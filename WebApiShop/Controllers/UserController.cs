using Microsoft.AspNetCore.Mvc;
using Services;
using Entities;
using DTOs;

namespace WebApiShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserServices _userServices;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserServices userServices, ILogger<UserController> logger)
        {
            _userServices = userServices;
            _logger = logger;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<UserDTO>>> Get()
        {
            IEnumerable<UserDTO> users = await _userServices.GetUsers();
            if (users != null && users.Any())
                return Ok(users);
            return NoContent();
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserDTO>> GetUserById(int id)
        {
            UserDTO user = await _userServices.GetUserById(id);
            if (user == null)
                return NotFound();
            return Ok(user);
        }

        [HttpPost("Login")]
        public async Task<ActionResult<UserDTO>> Login([FromBody] ExisitingUserDTO existingUser)
        {
            UserDTO user = await _userServices.Login(existingUser);
            if (user == null)
                return Unauthorized("Invalid email or password");

            _logger.LogInformation($"login attempted id:{user.UserId} email:{user.Email} first name:{user.FirstName} last name:{user.LastName}");
            return Ok(user);
        }

        [HttpPost]
        public async Task<ActionResult<UserDTO>> Register([FromBody] UserDTO user)
        {
            UserDTO user1 = await _userServices.Register(user);
            if (user1 == null)
                return BadRequest("Password too weak");

            
            return CreatedAtAction(nameof(GetUserById), new { id = user1.UserId }, user1);
        }

     

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] UserDTO updateUser)
        {
          
            bool success = await _userServices.Update(id, updateUser);
            if (!success) return BadRequest();
            return NoContent();
        }
    }
}