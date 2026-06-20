using API_Shopping.Context;
using API_Shopping.Controllers;
using API_Shopping.DTOs.Auth;
using API_Shopping.Exceptions.Auth;
using API_Shopping.Models;
using API_Shopping.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Shopping.Tests;

public class AuthServiceTest
{
    private readonly InMemoryDb _inMemoryDb;
    private readonly IConfiguration _configuration;

    public AuthServiceTest()
    {
        _inMemoryDb = new InMemoryDb();

        // Fake JWT configure
        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                { "JwtService:Key", "ThisIsASecretKeyForTestingPurposes123!" },
                { "JwtService:Duration", "60" }
            })
            .Build();
    }

    // Helper - Seeds a hashed user into the in-memory database
    private async Task<AppDbContext> SeedUserAsync(string email, string plainPassword, string role = "Customer")
    {
        var db = _inMemoryDb.GetInMemory();
        db.Users.Add(new User
        {
            Id = 1,
            Username = "testuser",
            Email = email,
            Password = BCrypt.Net.BCrypt.HashPassword(plainPassword),
            Role = role,
            IsActive = true,
            CreateAt = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
        return db;
    }

    // JwtService - FindUserWithLogin
    [Fact]
    public async Task FindUserWithLogin_ValidCredentials_ReturnsUser()
    {
        var db = await SeedUserAsync("test@email.com", "secret123");
        var service = new JwtService(db, _configuration);
        var login = new LoginDTO { Email = "test@email.com", Password = "secret123" };

        var result = await service.FindUserWithLogin(login);

        Assert.NotNull(result);
        Assert.Equal("test@email.com", result.Email);
    }

    [Fact]
    public async Task FindUserWithLogin_WrongPassword_ReturnsNull()
    {
        var db = await SeedUserAsync("test@email.com", "secret123");
        var service = new JwtService(db, _configuration);
        var login = new LoginDTO { Email = "test@email.com", Password = "wrongpassword" };

        var result = await service.FindUserWithLogin(login);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindUserWithLogin_EmailNotFound_ReturnsNull()
    {

        var db = await SeedUserAsync("test@email.com", "secret123");
        var service = new JwtService(db, _configuration);
        var login = new LoginDTO { Email = "notfound@email.com", Password = "secret123" };

        var result = await service.FindUserWithLogin(login);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindUserWithLogin_EmailIsCaseInsensitive_ReturnsUser()
    {

        var db = await SeedUserAsync("test@email.com", "secret123");
        var service = new JwtService(db, _configuration);
        var login = new LoginDTO { Email = "TEST@EMAIL.COM", Password = "secret123" };

        var result = await service.FindUserWithLogin(login);

        Assert.NotNull(result);
    }

    // JwtService - JWTGenerator
    [Fact]
    public async Task JWTGenerator_ValidUser_ReturnsNonEmptyToken()
    {
        var db = await SeedUserAsync("test@email.com", "secret123");
        var service = new JwtService(db, _configuration);
        var user = new User { Id = 1, Email = "test@email.com", Role = "Customer" };

        var token = service.JWTGenerator(user);

        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    // AuthController - Login
    [Fact]
    public async Task Login_ValidCredentials_ReturnsOkWithToken()
    {
        var db = await SeedUserAsync("test@email.com", "secret123", "Customer");
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
        var db = await SeedUserAsync("test@email.com", "secret123");
        var service = new JwtService(db, _configuration);
        var controller = new AuthController(service);
        var login = new LoginDTO { Email = "test@email.com", Password = "wrongpassword" };

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => controller.Login(login));
    }
}