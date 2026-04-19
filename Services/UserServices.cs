using Entities.Models;
using Repositories;
using DTOs;
using AutoMapper;

namespace Services
{
    public class UserServices : IUserServices
    {
        private const int MinimumPasswordScore = 2;
        private readonly IUserRepository _repository;
        private readonly IPasswordServices _passwordServices;
        private readonly IMapper _mapper;

        public UserServices(IUserRepository repository, IPasswordServices passwordServices, IMapper mapper)
        {
            _repository = repository;
            _passwordServices = passwordServices;
            _mapper = mapper;
        }

        public async Task<UserDTO?> GetUserById(int id)
        {
            return _mapper.Map<User?, UserDTO?>(await _repository.GetUserById(id));
        }

        public async Task<UserDTO?> Login(ExisitingUserDTO existingUser)
        {
            return _mapper.Map<User?, UserDTO?>(await _repository.Login(existingUser.Email, existingUser.Password));
        }

        public async Task<UserDTO?> Register(PostUserDTO user)
        {
            if (!IsPasswordStrong(user.Password))
                return null;

            User userEntity = _mapper.Map<User>(user);
            return _mapper.Map<UserDTO>(await _repository.Register(userEntity));
        }

        public async Task<bool> Update(int id, PostUserDTO updateUser)
        {
            if (!IsPasswordStrong(updateUser.Password))
                return false;

            User user = _mapper.Map<User>(updateUser);
            user.UserId = id;
            await _repository.Update(id, user);
            return true;
        }

        public async Task<IEnumerable<UserDTO>> GetUsers()
        {
            return _mapper.Map<IEnumerable<User>, IEnumerable<UserDTO>>(await _repository.GetUsers());
        }

        public async Task<bool> UserWithSameEmail(string email, int id = -1)
        {
            return await _repository.UserWithSameEmail(email, id);
        }

        public bool IsPasswordStrong(string password)
        {
            return _passwordServices.PasswordScore(password) >= MinimumPasswordScore;
        }
    }
}