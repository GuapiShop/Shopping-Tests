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

        [Fact]
        public async Task GetUserById_ExistingId_ReturnsUserDTO()
        {
            var db = new InMemoryDb().GetInMemory();
            var seeded = await TestDataSeeder.SeedUserAsync(db);
            var service = new UserService(db);

            var result = await service.GetUserById(seeded.Id);

            Assert.NotNull(result);
            Assert.Equal(seeded.Id, result.Id);
            Assert.Equal(seeded.Email, result.Email);
            Assert.Equal(seeded.Username, result.Username);
        }

        [Fact]
        public async Task GetUserById_NonExistingId_ThrowsUserNotFoundException()
        {
            var db = new InMemoryDb().GetInMemory();
            var service = new UserService(db);

            await Assert.ThrowsAsync<UserNotFoundException>(() => service.GetUserById(999));
        }

        [Fact]
        public async Task UpdateUser_ValidData_UpdatesUserSuccessfully()
        {
            var db = new InMemoryDb().GetInMemory();
            var seeded = await TestDataSeeder.SeedUserAsync(db);
            var service = new UserService(db);
            var dto = new UserUpdateDTO
            {
                Id = seeded.Id,
                Username = "updateduser",
                Email = "updated@email.com"
            };

            await service.UpdateUser(seeded.Id, dto);
            var updated = await service.GetUserById(seeded.Id);

            Assert.Equal("updateduser", updated.Username);
            Assert.Equal("updated@email.com", updated.Email);
            Assert.NotNull(updated.UpdateAt);
        }

        [Fact]
        public async Task UpdateUser_NonExistingId_ThrowsUserNotFoundException()
        {
            var db = new InMemoryDb().GetInMemory();
            var service = new UserService(db);
            var dto = new UserUpdateDTO
            {
                Id = 999,
                Username = "ghost",
                Email = "ghost@email.com"
            };

            await Assert.ThrowsAsync<UserNotFoundException>(() => service.UpdateUser(999, dto));
        }

        [Fact]
        public async Task UpdateUser_EmailTakenByAnotherUser_ThrowsUserAlreadyExistsException()
        {
            var db = new InMemoryDb().GetInMemory();
            var userA = await TestDataSeeder.SeedUserAsync(db, email: "userA@email.com");

            var userB = new API_Shopping.Models.User
            {
                Id = 2,
                Username = "userB",
                Email = "userB@email.com",
                Password = BCrypt.Net.BCrypt.HashPassword("pass"),
                Role = "client",
                IsActive = true,
                CreateAt = DateTime.UtcNow
            };
            db.Users.Add(userB);
            await db.SaveChangesAsync();

            var service = new UserService(db);
            var dto = new UserUpdateDTO
            {
                Id = userB.Id,
                Username = "userBupdated",
                Email = "userA@email.com"
            };

            await Assert.ThrowsAsync<UserAlreadyExistsException>(() => service.UpdateUser(userB.Id, dto));
        }

        [Fact]
        public async Task DisableUser_ActiveUser_DisablesSuccessfully()
        {
            var db = new InMemoryDb().GetInMemory();
            var seeded = await TestDataSeeder.SeedUserAsync(db);
            var service = new UserService(db);

            await service.DisableUser(seeded.Id);
            var result = await service.GetUserById(seeded.Id);

            Assert.False(result.IsActive);
        }

        [Fact]
        public async Task DisableUser_AlreadyDisabled_ThrowsUserAlreadyDisabledException()
        {
            var db = new InMemoryDb().GetInMemory();
            var seeded = await TestDataSeeder.SeedUserAsync(db);
            var service = new UserService(db);
            await service.DisableUser(seeded.Id);

            await Assert.ThrowsAsync<UserAlreadyDisabledException>(() => service.DisableUser(seeded.Id));
        }

        [Fact]
        public async Task DisableUser_NonExistingId_ThrowsUserNotFoundException()
        {
            var db = new InMemoryDb().GetInMemory();
            var service = new UserService(db);

            await Assert.ThrowsAsync<UserNotFoundException>(() => service.DisableUser(999));
        }

        [Fact]
        public async Task EnableUser_DisabledUser_EnablesSuccessfully()
        {
            var db = new InMemoryDb().GetInMemory();
            var seeded = await TestDataSeeder.SeedUserAsync(db);
            var service = new UserService(db);
            await service.DisableUser(seeded.Id);

            await service.EnableUser(seeded.Id);
            var result = await service.GetUserById(seeded.Id);

            Assert.True(result.IsActive);
        }

        [Fact]
        public async Task EnableUser_AlreadyActive_ThrowsUserAlreadyEnabledException()
        {
            var db = new InMemoryDb().GetInMemory();
            var seeded = await TestDataSeeder.SeedUserAsync(db);
            var service = new UserService(db);

            await Assert.ThrowsAsync<UserAlreadyEnabledException>(() => service.EnableUser(seeded.Id));
        }

        [Fact]
        public async Task EnableUser_NonExistingId_ThrowsUserNotFoundException()
        {
            var db = new InMemoryDb().GetInMemory();
            var service = new UserService(db);

            await Assert.ThrowsAsync<UserNotFoundException>(() => service.EnableUser(999));
        }

        [Fact]
        public async Task UserExists_ExistingId_ReturnsTrue()
        {
            var db = new InMemoryDb().GetInMemory();
            var seeded = await TestDataSeeder.SeedUserAsync(db);
            var service = new UserService(db);

            var exists = await service.UserExists(seeded.Id);

            Assert.True(exists);
        }

        [Fact]
        public async Task UserExists_NonExistingId_ReturnsFalse()
        {
            var db = new InMemoryDb().GetInMemory();
            var service = new UserService(db);

            var exists = await service.UserExists(999);

            Assert.False(exists);
        }

        [Fact]
        public async Task DeleteUser_ExistingId_RemovesUserFromDatabase()
        {
            var db = new InMemoryDb().GetInMemory();
            var seeded = await TestDataSeeder.SeedUserAsync(db);
            var service = new UserService(db);

            await service.DeleteUser(seeded.Id);

            var exists = await service.UserExists(seeded.Id);
            Assert.False(exists);
        }

        [Fact]
        public async Task DeleteUser_NonExistingId_ThrowsUserNotFoundException()
        {
            var db = new InMemoryDb().GetInMemory();
            var service = new UserService(db);

            await Assert.ThrowsAsync<UserNotFoundException>(() => service.DeleteUser(999));
        }

        [Fact]
        public async Task DeleteUser_ExistingId_DoesNotAffectOtherUsers()
        {
            var db = new InMemoryDb().GetInMemory();
            var userA = await TestDataSeeder.SeedUserAsync(db, email: "userA@email.com");
            var userB = new API_Shopping.Models.User
            {
                Id = 2,
                Username = "userB",
                Email = "userB@email.com",
                Password = BCrypt.Net.BCrypt.HashPassword("pass"),
                Role = "client",
                IsActive = true,
                CreateAt = DateTime.UtcNow
            };
            db.Users.Add(userB);
            await db.SaveChangesAsync();

            var service = new UserService(db);

            await service.DeleteUser(userA.Id);

            var userAExists = await service.UserExists(userA.Id);
            var userBExists = await service.UserExists(userB.Id);
            Assert.False(userAExists);
            Assert.True(userBExists);
        }

        [Fact]
        public async Task DeleteUser_ExistingId_GetUserByIdThrowsAfterDeletion()
        {
            var db = new InMemoryDb().GetInMemory();
            var seeded = await TestDataSeeder.SeedUserAsync(db);
            var service = new UserService(db);

            await service.DeleteUser(seeded.Id);

            await Assert.ThrowsAsync<UserNotFoundException>(() => service.GetUserById(seeded.Id));
        }
    }
}