using Entities.Models;
using Microsoft.EntityFrameworkCore;
using Repositories;

namespace TestProject
{
    public class DatabaseFixture : IDisposable
    {
        public ApiShopContext Context { get; private set; }
        private readonly string _databaseName;

        public DatabaseFixture()
        {
            _databaseName = $"ApiShopTest_{Guid.NewGuid():N}";

            // Uses SQL Server LocalDB by default (installed with Visual Studio).
            // Set TEST_SQL_SERVER to use another instance, e.g. "." or "localhost\SQLEXPRESS".
            var server = Environment.GetEnvironmentVariable("TEST_SQL_SERVER");
            if (string.IsNullOrWhiteSpace(server))
                server = @"(localdb)\MSSQLLocalDB";

            var options = new DbContextOptionsBuilder<ApiShopContext>()
                .UseSqlServer($"Data Source={server};Initial Catalog={_databaseName};Integrated Security=True;Pooling=False;TrustServerCertificate=True")
                .Options;

            Context = new ApiShopContext(options);
            Context.Database.EnsureCreated();
        }

        public void Dispose()
        {
            Context.Database.EnsureDeleted();
            Context.Dispose();
        }
    }
}