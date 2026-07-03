using API_Shopping.Enums;
using API_Shopping.Exceptions.Detail;
using API_Shopping.Exceptions.Product;
using API_Shopping.Exceptions.ShoppingCart;
using API_Shopping.Exceptions.User;
using API_Shopping.Models;
using API_Shopping.Services;
using Shopping.Tests.Helpers;

namespace Shopping.Tests.Services
{
    public class DetailServiceTest
    {
        [Fact]
        public async Task GetPendingOrders_UserNotFound_ThrowsDetailUserNotFoundException()
        {
            var db = new InMemoryDb().GetInMemory();
            var service = new DetailService(db);

            await Assert.ThrowsAsync<UserNotFoundException>(() =>
                service.GetPendingOrders(userId: 999));
        }

        [Fact]
        public async Task GetPendingOrders_NoPendingOrders_ThrowsDetailOrderNotFoundException()
        {
            var db = new InMemoryDb().GetInMemory();
            await TestDataSeeder.SeedUserAsync(db);
            var service = new DetailService(db);

            await Assert.ThrowsAsync<DetailOrderNotFoundException>(() =>
                service.GetPendingOrders(userId: 1));
        }

        [Fact]
        public async Task GetPendingOrders_WithPendingOrders_ReturnsCorrectCount()
        {
            var db = new InMemoryDb().GetInMemory();
            await TestDataSeeder.SeedUserAsync(db);
            await TestDataSeeder.SeedProductAsync(db, id: 1, quantity: 10, price: 500);
            await TestDataSeeder.SeedPendingOrderWithDetailsAsync(db, userId: 1, productId: 1);
            await TestDataSeeder.SeedPendingOrderWithDetailsAsync(db, userId: 1, productId: 1);

            var service = new DetailService(db);

            var result = await service.GetPendingOrders(userId: 1);

            Assert.Equal(2, result.Count);
        }

        [Fact]
        public async Task GetPendingOrders_WithPendingOrders_ReturnsCorrectItems()
        {
            var db = new InMemoryDb().GetInMemory();
            await TestDataSeeder.SeedUserAsync(db);
            await TestDataSeeder.SeedProductAsync(db, id: 1, quantity: 10, price: 500);
            await TestDataSeeder.SeedPendingOrderWithDetailsAsync(db, userId: 1, productId: 1, quantity: 3, price: 500);

            var service = new DetailService(db);

            var result = await service.GetPendingOrders(userId: 1);

            var order = result.First();
            Assert.Single(order.Items);
            Assert.Equal(1, order.Items[0].ProductId);
            Assert.Equal(3, order.Items[0].Quantity);
        }

        [Fact]
        public async Task GetPendingOrders_WithPendingOrders_ComputesGrandTotalCorrectly()
        {
            var db = new InMemoryDb().GetInMemory();
            await TestDataSeeder.SeedUserAsync(db);
            await TestDataSeeder.SeedProductAsync(db, id: 1, quantity: 10, price: 500);

            // 3 units * 500 = 1500
            await TestDataSeeder.SeedPendingOrderWithDetailsAsync(db, userId: 1, productId: 1, quantity: 3, price: 500);

            var service = new DetailService(db);

            var result = await service.GetPendingOrders(userId: 1);

            Assert.Equal(1500, result.First().GrandTotal);
        }

        [Fact]
        public async Task GetPendingOrders_OnlyReturnsPendingState_IgnoresOtherStates()
        {
            var db = new InMemoryDb().GetInMemory();
            await TestDataSeeder.SeedUserAsync(db);
            await TestDataSeeder.SeedProductAsync(db, id: 1, quantity: 10, price: 500);

            var completedOrder = new Order
            {
                UserId = 1,
                State = "completed",
                CreateAt = DateTime.UtcNow,
            };
            db.Orders.Add(completedOrder);
            await db.SaveChangesAsync();

            await TestDataSeeder.SeedPendingOrderWithDetailsAsync(db, userId: 1, productId: 1);

            var service = new DetailService(db);

            var result = await service.GetPendingOrders(userId: 1);

            Assert.Single(result);
            Assert.All(result, o => Assert.Equal("pending", o.State));
        }

        [Fact]
        public async Task GetPendingOrders_OnlyReturnsOrdersForRequestingUser()
        {
            var db = new InMemoryDb().GetInMemory();

            await TestDataSeeder.SeedUserAsync(db, email: "user1@email.com");
            var userB = new User
            {
                Id = 2,
                Username = "userB",
                Email = "user2@email.com",
                Password = BCrypt.Net.BCrypt.HashPassword("pass"),
                Role = "client",
                IsActive = true,
                CreateAt = DateTime.UtcNow,
            };
            db.Users.Add(userB);
            await db.SaveChangesAsync();

            await TestDataSeeder.SeedProductAsync(db, id: 1, quantity: 10, price: 500);

            await TestDataSeeder.SeedPendingOrderWithDetailsAsync(db, userId: 1, productId: 1);
            await TestDataSeeder.SeedPendingOrderWithDetailsAsync(db, userId: 2, productId: 1);

            var service = new DetailService(db);

            var result = await service.GetPendingOrders(userId: 1);

            Assert.Single(result);
            Assert.All(result, o => Assert.Equal(1, db.Orders.First(x => x.Id == o.OrderId).UserId));
        }

        [Fact]
        public async Task AddDetails_UserNotFound_ThrowsDetailUserNotFoundException()
        {
            var db = new InMemoryDb().GetInMemory();
            var service = new DetailService(db);

            await Assert.ThrowsAsync<UserNotFoundException>(() =>
                service.AddDetails(userId: 999));
        }

        [Fact]
        public async Task AddDetails_NoPendingCart_ThrowsCartItemNotFoundException()
        {
            var db = new InMemoryDb().GetInMemory();
            await TestDataSeeder.SeedUserAsync(db);
            var service = new DetailService(db);
            
            await Assert.ThrowsAsync<CartItemNotFoundException>(() =>
                service.AddDetails(userId: 1));
        }

        [Fact]
        public async Task AddDetails_EmptyCart_ThrowsEmptyCartException()
        {
            var db = new InMemoryDb().GetInMemory();
            await TestDataSeeder.SeedUserAsync(db);

            db.ShoppingCarts.Add(new ShoppingCart
            {
                UserId = 1,
                Status = ShoppingCartStatus.Pending,
                CreatedAt = DateTime.UtcNow,
                ItemShoppingCarts = new List<ItemShoppingCart>()
            });
            await db.SaveChangesAsync();

            var service = new DetailService(db);

            await Assert.ThrowsAsync<EmptyCartException>(() =>
                service.AddDetails(userId: 1));
        }

        [Fact]
        public async Task AddDetails_InactiveProduct_ThrowsDetailProductNotFoundException()
        {
            var db = new InMemoryDb().GetInMemory();
            await TestDataSeeder.SeedUserAsync(db);
            await TestDataSeeder.SeedProductAsync(db, id: 1, quantity: 10, isActive: false);
            await TestDataSeeder.SeedCartWithItemsAsync(db, userId: 1, items: new() { (1, 2) });

            var service = new DetailService(db);

            await Assert.ThrowsAsync<ProductNotFoundException>(() =>
                service.AddDetails(userId: 1));
        }

        [Fact]
        public async Task AddDetails_InsufficientStock_ThrowsDetailOutOfStockException()
        {
            var db = new InMemoryDb().GetInMemory();
            await TestDataSeeder.SeedUserAsync(db);
            await TestDataSeeder.SeedProductAsync(db, id: 1, quantity: 3);

            await TestDataSeeder.SeedCartWithItemsAsync(db, userId: 1, items: new() { (1, 10) });

            var service = new DetailService(db);

            await Assert.ThrowsAsync<OutOfStockException> (() =>
                service.AddDetails(userId: 1));
        }

        [Fact]
        public async Task AddDetails_ValidCart_CreatesOrderSuccessfully()
        {
            var db = new InMemoryDb().GetInMemory();
            await TestDataSeeder.SeedUserAsync(db);
            await TestDataSeeder.SeedProductAsync(db, id: 1, quantity: 10, price: 500);
            await TestDataSeeder.SeedCartWithItemsAsync(db, userId: 1, items: new() { (1, 2) });

            var service = new DetailService(db);

            var result = await service.AddDetails(userId: 1);

            Assert.NotNull(result);
            Assert.Equal(1, result.UserId);
            Assert.Equal("pending", result.State);
        }

        [Fact]
        public async Task AddDetails_ValidCart_CreatesDetailsForEachCartItem()
        {
            var db = new InMemoryDb().GetInMemory();
            await TestDataSeeder.SeedUserAsync(db);
            await TestDataSeeder.SeedProductAsync(db, id: 1, quantity: 10, price: 500);
            await TestDataSeeder.SeedProductAsync(db, id: 2, quantity: 10, price: 300);
            await TestDataSeeder.SeedCartWithItemsAsync(db, userId: 1, items: new() { (1, 2), (2, 3) });

            var service = new DetailService(db);

            var result = await service.AddDetails(userId: 1);

            var details = db.Details.Where(d => d.OrderId == result.Id).ToList();
            Assert.Equal(2, details.Count);
        }

        [Fact]
        public async Task AddDetails_ValidCart_ReducesProductStock()
        {
            var db = new InMemoryDb().GetInMemory();
            await TestDataSeeder.SeedUserAsync(db);
            await TestDataSeeder.SeedProductAsync(db, id: 1, quantity: 10, price: 500);
            await TestDataSeeder.SeedCartWithItemsAsync(db, userId: 1, items: new() { (1, 3) });

            var service = new DetailService(db);

            await service.AddDetails(userId: 1);

            var product = await db.Products.FindAsync(1L);
            Assert.Equal(7, product!.Quantity);
        }

        [Fact]
        public async Task AddDetails_ValidCart_ComputesDetailTotalsCorrectly()
        {
            var db = new InMemoryDb().GetInMemory();
            await TestDataSeeder.SeedUserAsync(db);
            await TestDataSeeder.SeedProductAsync(db, id: 1, quantity: 10, price: 500);
            await TestDataSeeder.SeedCartWithItemsAsync(db, userId: 1, items: new() { (1, 4) });

            var service = new DetailService(db);

            var result = await service.AddDetails(userId: 1);

            var detail = db.Details.First(d => d.OrderId == result.Id);
            Assert.Equal(2000, detail.Total);
        }

        [Fact]
        public async Task AddDetails_ValidCart_ClearsCartItemsAfterProcessing()
        {
            var db = new InMemoryDb().GetInMemory();
            await TestDataSeeder.SeedUserAsync(db);
            await TestDataSeeder.SeedProductAsync(db, id: 1, quantity: 10, price: 500);
            var cart = await TestDataSeeder.SeedCartWithItemsAsync(db, userId: 1, items: new() { (1, 2) });

            var service = new DetailService(db);

            await service.AddDetails(userId: 1);

            var remainingItems = db.ItemShoppingCarts
                .Where(i => i.shoppingCartId == cart.Id)
                .ToList();
            Assert.Empty(remainingItems);
        }

        [Fact]
        public async Task AddDetails_ValidCart_DeletesCartAfterProcessing()
        {
            var db = new InMemoryDb().GetInMemory();
            await TestDataSeeder.SeedUserAsync(db);
            await TestDataSeeder.SeedProductAsync(db, id: 1, quantity: 10, price: 500);
            var cart = await TestDataSeeder.SeedCartWithItemsAsync(db, userId: 1, items: new() { (1, 2) });

            var service = new DetailService(db);

            await service.AddDetails(userId: 1);

            var deletedCart = await db.ShoppingCarts.FindAsync(cart.Id);
            Assert.Null(deletedCart);
        }
    }
}