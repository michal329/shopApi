using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Org.BouncyCastle.Crypto.Generators;

namespace Repositories
{
    public class UserRepository : IUserRepository
    {
        private readonly ApiShopContext _context;

        public UserRepository(ApiShopContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<User>> GetUsers()
        {
            return await _context.Users.ToListAsync();
        }

        public async Task<User?> GetUserById(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        public async Task<User?> Login(string email, string password)
        {
            var user = await _context.Users
                .Include(u => u.Orders)
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
                return null;

            bool passwordMatch = BCrypt.Net.BCrypt.Verify(password, user.Password);
            return passwordMatch ? user : null;
        }

        public async Task<User?> Register(User user)
        {
            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task Update(int id, User updateUser)
        {
            var currentUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == id);

            if (currentUser != null && updateUser.Password != currentUser.Password)
            {
                updateUser.Password = BCrypt.Net.BCrypt.HashPassword(updateUser.Password);
            }

            _context.Users.Update(updateUser);
            await _context.SaveChangesAsync();
        }

        public async Task<bool> UserWithSameEmail(string email, int id)
        {
            User? userWithSameEmail;

            if (id < 0)
            {
                userWithSameEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email);
            }
            else
            {
                userWithSameEmail = await _context.Users
                    .FirstOrDefaultAsync(u => u.Email == email && u.UserId != id);
            }

            return userWithSameEmail == null;
        }
    }
}