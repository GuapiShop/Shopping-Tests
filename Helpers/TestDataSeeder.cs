using API_Shopping.Context;
using API_Shopping.Models;

namespace Shopping.Tests.Helpers
{
    public static class TestDataSeeder
    {
        public static async Task<User> SeedUserAsync(
            AppDbContext db,
            string email = "test@email.com",
            string plainPassword = "secret123",
            string role = "Customer")
        {
            var user = new User
            {
                Id = 1,
                Username = "testuser",
                Email = email,
                Password = BCrypt.Net.BCrypt.HashPassword(plainPassword),
                Role = role,
                IsActive = true,
                CreateAt = DateTime.UtcNow
            };
            db.Users.Add(user);
            await db.SaveChangesAsync();
            return user;
        }
    }
}