using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.RateLimiting;
using Services;
using DTOs;
using WebApiShop.Auth;

namespace WebApiShop.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class UserController : ControllerBase
    {
        private readonly IUserServices _userServices;
        private readonly IAuthService _authService;
        private readonly AuthCookie _authCookie;
        private readonly ILogger<UserController> _logger;

        public UserController(IUserServices userServices, IAuthService authService,
            AuthCookie authCookie, ILogger<UserController> logger)
        {
            _userServices = userServices;
            _authService = authService;
            _authCookie = authCookie;
            _logger = logger;
        }

        [HttpGet]
        [AuthorizeRoles(Roles.Admin)]
        public async Task<ActionResult<IEnumerable<UserDTO>>> Get()
        {
            IEnumerable<UserDTO> users = await _userServices.GetUsers();
            if (users != null && users.Any())
                return Ok(users);
            return NoContent();
        }

        // The signed-in user. The token is in an HTTP-only cookie the client
        // can't read, so this is how the client finds out who is logged in.
        [HttpGet("Me")]
        public async Task<ActionResult<UserDTO>> Me()
        {
            UserDTO? user = await _userServices.GetUserById(User.GetUserId());
            if (user == null)
                return NotFound();
            return Ok(user);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<UserDTO>> GetUserById(int id)
        {
            if (!User.CanAccessUser(id))
                return Forbid();

            UserDTO? user = await _userServices.GetUserById(id);
            if (user == null)
                return NotFound();
            return Ok(user);
        }

        [HttpPost("Login")]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        public async Task<ActionResult<UserDTO>> Login([FromBody] ExisitingUserDTO existingUser)
        {
            AuthResult? result = await _authService.Login(existingUser);
            if (result == null)
                return Unauthorized("Invalid email or password");

            _authCookie.Append(Response, result.AccessToken);
            _logger.LogInformation("Login - Id:{UserId} Email:{Email}", result.User.UserId, result.User.Email);
            return Ok(result.User);
        }

        [HttpPost]
        [AllowAnonymous]
        [EnableRateLimiting(RateLimitPolicies.Auth)]
        public async Task<ActionResult<UserDTO>> Register([FromBody] PostUserDTO newUser)
        {
            if (!await _userServices.UserWithSameEmail(newUser.Email))
                return BadRequest("The email already exists. Please try again.");

            if (!_userServices.IsPasswordStrong(newUser.Password))
                return BadRequest("The password is too weak. Please try again.");

            AuthResult? result = await _authService.Register(newUser);
            if (result == null)
                return BadRequest();

            _authCookie.Append(Response, result.AccessToken);
            return CreatedAtAction(nameof(GetUserById), new { id = result.User.UserId }, result.User);
        }

        [HttpPost("Logout")]
        [AllowAnonymous]
        public IActionResult Logout()
        {
            _authCookie.Delete(Response);
            return NoContent();
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> Update(int id, [FromBody] PostUserDTO updateUser)
        {
            if (!User.CanAccessUser(id))
                return Forbid();

            if (!await _userServices.UserWithSameEmail(updateUser.Email, id))
                return BadRequest("The email already exists. Please try again.");

            if (!_userServices.IsPasswordStrong(updateUser.Password))
                return BadRequest("The password is too weak. Please try again.");

            bool success = await _userServices.Update(id, updateUser);
            if (!success) return BadRequest();
            return NoContent();
        }
    }
}
