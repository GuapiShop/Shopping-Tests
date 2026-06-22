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

        public static async Task<Product> SeedProductAsync(
            AppDbContext db,
            long id = 1,
            string name = "Test Product",
            string category = "Electronics",
            decimal price = 9999,
            decimal taxCabys = 0.13m,
            int quantity = 10,
            bool isActive = true)
        {
            var product = new Product
            {
                Id = id,
                Name = name,
                Description = "A test product description",
                Category = category,
                CodeCabys = "123456",
                DescriptionCabys = "Test CABYS description",
                Price = price,
                TaxCabys = taxCabys,
                Quantity = quantity,
                IsActive = isActive,
                CreateAt = DateTime.UtcNow
            };
            db.Products.Add(product);
            await db.SaveChangesAsync();
            return product;
        }
    }
}