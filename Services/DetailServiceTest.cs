using API_Shopping.Exceptions.Detail;
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
    }
}