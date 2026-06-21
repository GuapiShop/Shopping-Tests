using API_Shopping.Controllers;
using API_Shopping.DTOs.Auth;
using API_Shopping.Exceptions.Auth;
using API_Shopping.Services;
using Microsoft.AspNetCore.Mvc;
using Shopping.Tests.Helpers;

namespace Shopping.Tests.Controllers;

public class AuthControllerTest
{
    private readonly InMemoryDb _inMemoryDb;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

    public AuthControllerTest()
    {
        _inMemoryDb = new InMemoryDb();
        _configuration = TestConfiguration.Build();
    }

    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithToken()
    {
        var db = _inMemoryDb.GetInMemory();
        await TestDataSeeder.SeedUserAsync(db, role: "Customer");
        var service = new JwtService(db, _configuration);
        var controller = new AuthController(service);
        var login = new LoginDTO { Email = "test@email.com", Password = "secret123" };

        var result = await controller.Login(login);

        var ok = Assert.IsType<OkObjectResult>(result.Result);
        var value = ok.Value!;
        var token = value.GetType().GetProperty("token")?.GetValue(value)?.ToString();
        var role = value.GetType().GetProperty("role")?.GetValue(value)?.ToString();

        Assert.NotNull(token);
        Assert.NotEmpty(token);
        Assert.Equal("Customer", role);
    }

    [Fact]
    public async Task Login_EmptyEmail_ThrowsEmptyEmailException()
    {
        var db = _inMemoryDb.GetInMemory();
        var service = new JwtService(db, _configuration);
        var controller = new AuthController(service);
        var login = new LoginDTO { Email = "", Password = "secret123" };

        await Assert.ThrowsAsync<EmptyEmailException>(() => controller.Login(login));
    }

    [Fact]
    public async Task Login_EmptyPassword_ThrowsEmptyPasswordException()
    {
        var db = _inMemoryDb.GetInMemory();
        var service = new JwtService(db, _configuration);
        var controller = new AuthController(service);
        var login = new LoginDTO { Email = "test@email.com", Password = "" };

        await Assert.ThrowsAsync<EmptyPasswordException>(() => controller.Login(login));
    }

    [Fact]
    public async Task Login_InvalidCredentials_ThrowsInvalidCredentialsException()
    {
        var db = _inMemoryDb.GetInMemory();
        await TestDataSeeder.SeedUserAsync(db);
        var service = new JwtService(db, _configuration);
        var controller = new AuthController(service);
        var login = new LoginDTO { Email = "test@email.com", Password = "wrongpassword" };

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => controller.Login(login));
    }
}