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
            // Bug 6 Fix: fetch by email only, then verify password with BCrypt
            // (can't compare hashed passwords directly in a SQL query)
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
            // Bug 6 Fix: hash the password before saving
            user.Password = BCrypt.Net.BCrypt.HashPassword(user.Password);

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();
            return user;
        }

        public async Task Update(int id, User updateUser)
        {
            var currentUser = await _context.Users.AsNoTracking().FirstOrDefaultAsync(u => u.UserId == id);

            if (currentUser != null)
            {
                updateUser.UserId = id;

                if (updateUser.Password != currentUser.Password)
                {
                    updateUser.Password = BCrypt.Net.BCrypt.HashPassword(updateUser.Password);
                }
            }

            _context.Users.Update(updateUser);
            await _context.SaveChangesAsync();
        }
    }
}