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

        [Fact]
        public async Task UpdateProductItemFromCart_NegativeQuantity_ThrowsInvalidQuantityException()
        {
            var db = new InMemoryDb().GetInMemory();
            var service = new ShoppingCartService(db);

            await Assert.ThrowsAsync<InvalidQuantityException>(() =>
                service.UpdateProductItemFromCart(itemId: 1, quantity: -1, userId: 1));
        }

        [Fact]
        public async Task UpdateProductItemFromCart_ItemNotFound_ThrowsCartItemNotFoundException()
        {
            var db = new InMemoryDb().GetInMemory();
            var service = new ShoppingCartService(db);

            await Assert.ThrowsAsync<CartItemNotFoundException>(() =>
                service.UpdateProductItemFromCart(itemId: 999, quantity: 1, userId: 1));
        }

        [Fact]
        public async Task UpdateProductItemFromCart_ValidQuantity_UpdatesItemQuantity()
        {
            var db = new InMemoryDb().GetInMemory();
            var product = await TestDataSeeder.SeedProductAsync(db, id: 1, quantity: 10);
            var cart = await TestDataSeeder.SeedCartAsync(db, userId: 1);
            var item = await TestDataSeeder.SeedCartItemAsync(db, cartId: cart.Id, productId: product.Id, quantity: 2);
            var service = new ShoppingCartService(db);

            var result = await service.UpdateProductItemFromCart(item.Id, quantity: 5, userId: 1);

            Assert.True(result);
            var updated = db.ItemShoppingCarts.Find(item.Id);
            Assert.Equal(5, updated!.Quantity);
        }

        [Fact]
        public async Task UpdateProductItemFromCart_QuantityZero_RemovesItemFromCart()
        {
            var db = new InMemoryDb().GetInMemory();
            var product = await TestDataSeeder.SeedProductAsync(db, id: 1, quantity: 10);
            var cart = await TestDataSeeder.SeedCartAsync(db, userId: 1);
            var item = await TestDataSeeder.SeedCartItemAsync(db, cartId: cart.Id, productId: product.Id, quantity: 2);
            var service = new ShoppingCartService(db);

            var result = await service.UpdateProductItemFromCart(item.Id, quantity: 0, userId: 1);

            Assert.True(result);
            var deleted = db.ItemShoppingCarts.Find(item.Id);
            Assert.Null(deleted);
        }

        [Fact]
        public async Task UpdateProductItemFromCart_ExceedsStock_ThrowsInsufficientStockException()
        {
            var db = new InMemoryDb().GetInMemory();
            var product = await TestDataSeeder.SeedProductAsync(db, id: 1, quantity: 5);
            var cart = await TestDataSeeder.SeedCartAsync(db, userId: 1);
            var item = await TestDataSeeder.SeedCartItemAsync(db, cartId: cart.Id, productId: product.Id, quantity: 2);
            var service = new ShoppingCartService(db);

            await Assert.ThrowsAsync<InsufficientStockException>(() =>
                service.UpdateProductItemFromCart(item.Id, quantity: 10, userId: 1));
        }

        [Fact]
        public async Task UpdateProductItemFromCart_WrongUserId_ThrowsCartItemNotFoundException()
        {
            var db = new InMemoryDb().GetInMemory();
            var product = await TestDataSeeder.SeedProductAsync(db, id: 1, quantity: 10);
            var cart = await TestDataSeeder.SeedCartAsync(db, userId: 1);
            var item = await TestDataSeeder.SeedCartItemAsync(db, cartId: cart.Id, productId: product.Id, quantity: 2);
            var service = new ShoppingCartService(db);

            //userId 2 cannot touch userId 1 cart item
            await Assert.ThrowsAsync<CartItemNotFoundException>(() =>
                service.UpdateProductItemFromCart(item.Id, quantity: 1, userId: 2));
        }

        [Fact]
        public async Task DeleteProductItemFromCart_ExistingItem_RemovesItemSuccessfully()
        {
            var db = new InMemoryDb().GetInMemory();
            var product = await TestDataSeeder.SeedProductAsync(db, id: 1, quantity: 10);
            var cart = await TestDataSeeder.SeedCartAsync(db, userId: 1);
            var item = await TestDataSeeder.SeedCartItemAsync(db, cartId: cart.Id, productId: product.Id);
            var service = new ShoppingCartService(db);

            var result = await service.DeleteProductItemFromCart(item.Id, userId: 1);

            Assert.True(result);
            var deleted = db.ItemShoppingCarts.Find(item.Id);
            Assert.Null(deleted);
        }

        [Fact]
        public async Task DeleteProductItemFromCart_ItemNotFound_ThrowsCartItemNotFoundException()
        {
            var db = new InMemoryDb().GetInMemory();
            var service = new ShoppingCartService(db);

            await Assert.ThrowsAsync<CartItemNotFoundException>(() =>
                service.DeleteProductItemFromCart(itemId: 999, userId: 1));
        }

        [Fact]
        public async Task DeleteProductItemFromCart_WrongUserId_ThrowsCartItemNotFoundException()
        {
            var db = new InMemoryDb().GetInMemory();
            var product = await TestDataSeeder.SeedProductAsync(db, id: 1, quantity: 10);
            var cart = await TestDataSeeder.SeedCartAsync(db, userId: 1);
            var item = await TestDataSeeder.SeedCartItemAsync(db, cartId: cart.Id, productId: product.Id);
            var service = new ShoppingCartService(db);

            // userId 2 cannot delete userId 1 item
            await Assert.ThrowsAsync<CartItemNotFoundException>(() =>
                service.DeleteProductItemFromCart(item.Id, userId: 2));
        }

        [Fact]
        public async Task DeleteProductItemFromCart_ExistingItem_DoesNotAffectOtherItems()
        {
            var db = new InMemoryDb().GetInMemory();
            var productA = await TestDataSeeder.SeedProductAsync(db, id: 1, quantity: 10);
            var productB = await TestDataSeeder.SeedProductAsync(db, id: 2, quantity: 10);
            var cart = await TestDataSeeder.SeedCartAsync(db, userId: 1);
            var itemA = await TestDataSeeder.SeedCartItemAsync(db, cartId: cart.Id, productId: productA.Id);
            var itemB = await TestDataSeeder.SeedCartItemAsync(db, cartId: cart.Id, productId: productB.Id);
            var service = new ShoppingCartService(db);

            await service.DeleteProductItemFromCart(itemA.Id, userId: 1);

            var itemAExists = db.ItemShoppingCarts.Find(itemA.Id);
            var itemBExists = db.ItemShoppingCarts.Find(itemB.Id);
            Assert.Null(itemAExists);
            Assert.NotNull(itemBExists);
        }
    }
}