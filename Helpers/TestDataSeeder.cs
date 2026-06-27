using API_Shopping.Context;
using API_Shopping.Enums;
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

        public static async Task<ShoppingCart> SeedCartAsync(
            AppDbContext db,
            long userId = 1)
        {
            var cart = new ShoppingCart
            {
                Status = ShoppingCartStatus.Pending,
                UserId = userId,
                CreatedAt = DateTime.UtcNow,
                ItemShoppingCarts = new List<ItemShoppingCart>()
            };
            db.ShoppingCarts.Add(cart);
            await db.SaveChangesAsync();
            return cart;
        }

        public static async Task<ItemShoppingCart> SeedCartItemAsync(
            AppDbContext db,
            long cartId,
            long productId,
            int quantity = 2,
            int unitPrice = 100)
        {
            var item = new ItemShoppingCart
            {
                shoppingCartId = cartId,
                productId = productId,
                Quantity = quantity,
                UnitPrice = unitPrice,
            };
            db.ItemShoppingCarts.Add(item);
            await db.SaveChangesAsync();
            return item;
        }
    }
}