using API_Shopping.Context;
using Microsoft.EntityFrameworkCore;

namespace Shopping.Tests.Helpers
{
    public class InMemoryDb
    {
        public AppDbContext GetInMemory()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString()) // Secluded tests
                .Options;
            return new AppDbContext(options);
        }
    }
}