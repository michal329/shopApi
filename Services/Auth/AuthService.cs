using DTOs;

namespace Services
{
    /// <summary>The user and the access token to give them.</summary>
    public record AuthResult(UserDTO User, AccessToken AccessToken);

    public interface IAuthService
    {
        /// <summary>Null when the email or password is wrong.</summary>
        Task<AuthResult?> Login(ExisitingUserDTO credentials);

        /// <summary>Null when the user could not be registered (for example, a weak password).</summary>
        Task<AuthResult?> Register(PostUserDTO newUser);
    }

    /// <summary>
    /// Login and registration both end with the user signed in, so both
    /// issue a token here, in the service layer. The controller only decides
    /// how to deliver it (an HTTP-only cookie).
    /// </summary>
    public class AuthService : IAuthService
    {
        private readonly IUserServices _userServices;
        private readonly ITokenService _tokenService;

        public AuthService(IUserServices userServices, ITokenService tokenService)
        {
            _userServices = userServices;
            _tokenService = tokenService;
        }

        public async Task<AuthResult?> Login(ExisitingUserDTO credentials)
        {
            UserDTO? user = await _userServices.Login(credentials);
            return user == null ? null : new AuthResult(user, _tokenService.CreateToken(user));
        }

        public async Task<AuthResult?> Register(PostUserDTO newUser)
        {
            UserDTO? user = await _userServices.Register(newUser);
            return user == null ? null : new AuthResult(user, _tokenService.CreateToken(user));
        }
    }
}
