using API_Shopping.DTOs.User;
using API_Shopping.Exceptions.User;
using API_Shopping.Services;
using Shopping.Tests.Helpers;

namespace Shopping.Tests.Services
{
    public class UserServiceTest
    {
        [Fact]
        public async Task AddUser_ValidData_ReturnsCreatedUser()
        {
            var db = new InMemoryDb().GetInMemory();
            var service = new UserService(db);
            var dto = new UserCreateDTO
            {
                Username = "newuser",
                Email = "new@email.com",
                Password = "password123"
            };

            var result = await service.AddUser(dto);

            Assert.NotNull(result);
            Assert.Equal(dto.Username, result.Username);
            Assert.Equal(dto.Email, result.Email);
            Assert.Equal("client", result.Role);
            Assert.True(result.IsActive);
            Assert.NotEqual(dto.Password, result.Password);
        }

        [Fact]
        public async Task AddUser_DuplicateEmail_ThrowsUserAlreadyExistsException()
        {
            var db = new InMemoryDb().GetInMemory();
            await TestDataSeeder.SeedUserAsync(db, email: "taken@email.com");
            var service = new UserService(db);
            var dto = new UserCreateDTO
            {
                Username = "differentuser",
                Email = "taken@email.com",
                Password = "password123"
            };

            await Assert.ThrowsAsync<UserAlreadyExistsException>(() => service.AddUser(dto));
        }

        [Fact]
        public async Task AddUser_DuplicateUsername_ThrowsUserAlreadyExistsException()
        {
            var db = new InMemoryDb().GetInMemory();
            await TestDataSeeder.SeedUserAsync(db, email: "unique@email.com");
            var service = new UserService(db);
            var dto = new UserCreateDTO
            {
                Username = "testuser",
                Email = "another@email.com",
                Password = "password123"
            };

            await Assert.ThrowsAsync<UserAlreadyExistsException>(() => service.AddUser(dto));
        }

        [Fact]
        public async Task GetUsers_ValidPagination_ReturnsPaginatedResult()
        {
            var db = new InMemoryDb().GetInMemory();
            await TestDataSeeder.SeedUserAsync(db);
            var service = new UserService(db);

            var result = await service.GetUsers(page: 1, pageSize: 10);

            Assert.NotNull(result);

            var type = result.GetType();
            var data = type.GetProperty("data")?.GetValue(result) as IEnumerable<object>;
            Assert.NotNull(data);
            Assert.NotEmpty(data);
        }

        [Fact]
        public async Task GetUsers_InvalidPage_ThrowsInvalidPaginationException()
        {
            var db = new InMemoryDb().GetInMemory();
            var service = new UserService(db);

            await Assert.ThrowsAsync<InvalidPaginationException>(() => service.GetUsers(page: 0, pageSize: 10));
        }

        [Fact]
        public async Task GetUsers_InvalidPageSize_ThrowsInvalidPaginationException()
        {
            var db = new InMemoryDb().GetInMemory();
            var service = new UserService(db);

            await Assert.ThrowsAsync<InvalidPaginationException>(() => service.GetUsers(page: 1, pageSize: 0));
        }
    }
}