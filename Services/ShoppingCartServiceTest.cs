using API_Shopping.DTOs.ShoppingCart;
using API_Shopping.Enums;
using API_Shopping.Exceptions.Product;
using API_Shopping.Exceptions.ShoppingCart;
using API_Shopping.Services;
using Shopping.Tests.Helpers;

namespace Shopping.Tests.Services
{
    public class ShoppingCartServiceTest
    {
        [Fact]
        public async Task GetOrCreateShoppingCart_InvalidUserId_ThrowsInvalidUserIdException()
        {
            var db = new InMemoryDb().GetInMemory();
            var service = new ShoppingCartService(db);

            await Assert.ThrowsAsync<InvalidUserIdException>(() =>
                service.GetOrCreateShoppingCart(0));
        }

        [Fact]
        public async Task GetOrCreateShoppingCart_NoPendingCart_CreatesNewCart()
        {
            var db = new InMemoryDb().GetInMemory();
            var service = new ShoppingCartService(db);

            var result = await service.GetOrCreateShoppingCart(1);

            Assert.NotNull(result);
            Assert.Equal(1, result.UserId);
            Assert.Equal(ShoppingCartStatus.Pending, result.Status);
        }

        [Fact]
        public async Task GetOrCreateShoppingCart_ExistingPendingCart_ReturnsExistingCart()
        {
            var db = new InMemoryDb().GetInMemory();
            var seeded = await TestDataSeeder.SeedCartAsync(db, userId: 1);
            var service = new ShoppingCartService(db);

            var result = await service.GetOrCreateShoppingCart(1);

            Assert.Equal(seeded.Id, result.Id);

            var cartCount = db.ShoppingCarts.Count();
            Assert.Equal(1, cartCount);
        }

        [Fact]
        public async Task AddProductIntoCart_InvalidQuantity_ThrowsInvalidQuantityException()
        {
            var db = new InMemoryDb().GetInMemory();
            var service = new ShoppingCartService(db);
            var dto = new ItemShoppingCartCreateDTO
            {
                ProductId = 1,
                Quantity = 0,
                UnitPrice = 100
            };

            await Assert.ThrowsAsync<InvalidQuantityException>(() =>
                service.AddProductIntoCart(dto, userId: 1));
        }

        [Fact]
        public async Task AddProductIntoCart_ProductNotFound_ThrowsProductNotFoundException()
        {
            var db = new InMemoryDb().GetInMemory();
            var service = new ShoppingCartService(db);
            var dto = new ItemShoppingCartCreateDTO
            {
                ProductId = 999,
                Quantity = 1,
                UnitPrice = 100
            };

            await Assert.ThrowsAsync<ProductNotFoundException>(() =>
                service.AddProductIntoCart(dto, userId: 1));
        }

        [Fact]
        public async Task AddProductIntoCart_InactiveProduct_ThrowsProductNotFoundException()
        {
            var db = new InMemoryDb().GetInMemory();
            await TestDataSeeder.SeedProductAsync(db, id: 1, quantity: 10, isActive: false);
            var service = new ShoppingCartService(db);
            var dto = new ItemShoppingCartCreateDTO
            {
                ProductId = 1,
                Quantity = 1,
                UnitPrice = 100
            };

            await Assert.ThrowsAsync<ProductNotFoundException>(() =>
                service.AddProductIntoCart(dto, userId: 1));
        }

        [Fact]
        public async Task AddProductIntoCart_ExceedsStock_ThrowsInsufficientStockException()
        {
            var db = new InMemoryDb().GetInMemory();
            await TestDataSeeder.SeedProductAsync(db, id: 1, quantity: 5);
            var service = new ShoppingCartService(db);
            var dto = new ItemShoppingCartCreateDTO
            {
                ProductId = 1,
                Quantity = 10,
                UnitPrice = 100
            };

            await Assert.ThrowsAsync<InsufficientStockException>(() =>
                service.AddProductIntoCart(dto, userId: 1));
        }

        [Fact]
        public async Task AddProductIntoCart_NewItem_AddsItemToCart()
        {
            var db = new InMemoryDb().GetInMemory();
            await TestDataSeeder.SeedProductAsync(db, id: 1, quantity: 10);
            var service = new ShoppingCartService(db);
            var dto = new ItemShoppingCartCreateDTO
            {
                ProductId = 1,
                Quantity = 2,
                UnitPrice = 100
            };

            var result = await service.AddProductIntoCart(dto, userId: 1);

            Assert.NotNull(result);
            Assert.Single(result.ItemShoppingCarts);
            Assert.Equal(2, result.ItemShoppingCarts[0].Quantity);
        }

        [Fact]
        public async Task AddProductIntoCart_ExistingItem_AccumulatesQuantity()
        {
            var db = new InMemoryDb().GetInMemory();
            await TestDataSeeder.SeedProductAsync(db, id: 1, quantity: 10);
            var cart = await TestDataSeeder.SeedCartAsync(db, userId: 1);
            await TestDataSeeder.SeedCartItemAsync(db, cartId: cart.Id, productId: 1, quantity: 3);
            var service = new ShoppingCartService(db);
            var dto = new ItemShoppingCartCreateDTO
            {
                ProductId = 1,
                Quantity = 4, // 3 + 4 = 7, within stock of 10
                UnitPrice = 100
            };

            var result = await service.AddProductIntoCart(dto, userId: 1);

            Assert.Single(result.ItemShoppingCarts);
            Assert.Equal(7, result.ItemShoppingCarts[0].Quantity);
        }

        [Fact]
        public async Task AddProductIntoCart_ExistingItemExceedsStock_ThrowsInsufficientStockException()
        {
            var db = new InMemoryDb().GetInMemory();
            await TestDataSeeder.SeedProductAsync(db, id: 1, quantity: 5);
            var cart = await TestDataSeeder.SeedCartAsync(db, userId: 1);
            await TestDataSeeder.SeedCartItemAsync(db, cartId: cart.Id, productId: 1, quantity: 4);
            var service = new ShoppingCartService(db);
            var dto = new ItemShoppingCartCreateDTO
            {
                ProductId = 1,
                Quantity = 3, // 4 + 3 = 7, exceeds stock of 5
                UnitPrice = 100
            };

            await Assert.ThrowsAsync<InsufficientStockException>(() =>
                service.AddProductIntoCart(dto, userId: 1));
        }
    }
}