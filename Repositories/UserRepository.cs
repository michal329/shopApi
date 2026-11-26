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

            if (IsBCryptHash(user.Password))
                return BCrypt.Net.BCrypt.Verify(password, user.Password) ? user : null;

            // Accounts created before passwords were hashed still store the
            // password as plain text (BCrypt.Verify would throw on it). Check
            // it in constant time, and on a successful login replace it with a
            // BCrypt hash, so these accounts are upgraded as their users log in.
            if (!PlainTextEquals(password, user.Password))
                return null;

            user.Password = BCrypt.Net.BCrypt.HashPassword(password);
            await _context.SaveChangesAsync();
            return user;
        }

        // A BCrypt hash looks like "$2a$11$" + 53 characters.
        private static bool IsBCryptHash(string stored) =>
            stored.Length == 60 && stored.StartsWith("$2") && stored[3] == '$';

        private static bool PlainTextEquals(string given, string stored) =>
            System.Security.Cryptography.CryptographicOperations.FixedTimeEquals(
                System.Text.Encoding.UTF8.GetBytes(given), System.Text.Encoding.UTF8.GetBytes(stored));

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

            if (currentUser != null)
            {
                if (updateUser.Password != currentUser.Password)
                    updateUser.Password = BCrypt.Net.BCrypt.HashPassword(updateUser.Password);

                // The update DTO doesn't carry these, so keep the stored values
                // (otherwise an admin who edits their profile would lose admin).
                updateUser.IsAdmin = currentUser.IsAdmin;
                updateUser.Phone ??= currentUser.Phone;
                updateUser.Address ??= currentUser.Address;
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