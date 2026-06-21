using API_Shopping.DTOs.Auth;
using API_Shopping.Exceptions.Auth;
using API_Shopping.Models;
using API_Shopping.Services;
using Microsoft.IdentityModel.Tokens;
using Shopping.Tests.Helpers;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace Shopping.Tests.Services;

public class JwtServiceTest
{
    private readonly InMemoryDb _inMemoryDb;
    private readonly Microsoft.Extensions.Configuration.IConfiguration _configuration;

    public JwtServiceTest()
    {
        _inMemoryDb = new InMemoryDb();
        _configuration = TestConfiguration.Build();
    }

    // Helper - Builds a JWT signed with a given key and expiration, bypassing JwtService
    private static string BuildToken(string key, DateTime expires, string algorithm = SecurityAlgorithms.HmacSha256Signature)
    {
        var claims = new[]
        {
            new Claim(ClaimTypes.NameIdentifier, "1"),
            new Claim(ClaimTypes.Email, "test@email.com"),
            new Claim(ClaimTypes.Role, "Customer"),
        };

        var securityKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(key));
        var credentials = new SigningCredentials(securityKey, algorithm);

        var token = new JwtSecurityToken(
            claims: claims,
            expires: expires,
            signingCredentials: credentials
        );

        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    [Fact]
    public async Task FindUserWithLogin_ValidCredentials_ReturnsUser()
    {
        var db = _inMemoryDb.GetInMemory();
        await TestDataSeeder.SeedUserAsync(db);
        var service = new JwtService(db, _configuration);
        var login = new LoginDTO { Email = "test@email.com", Password = "secret123" };

        var result = await service.FindUserWithLogin(login);

        Assert.NotNull(result);
        Assert.Equal("test@email.com", result.Email);
    }

    [Fact]
    public async Task FindUserWithLogin_WrongPassword_ReturnsNull()
    {
        var db = _inMemoryDb.GetInMemory();
        await TestDataSeeder.SeedUserAsync(db);
        var service = new JwtService(db, _configuration);
        var login = new LoginDTO { Email = "test@email.com", Password = "wrongpassword" };

        var result = await service.FindUserWithLogin(login);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindUserWithLogin_EmailNotFound_ReturnsNull()
    {
        var db = _inMemoryDb.GetInMemory();
        await TestDataSeeder.SeedUserAsync(db);
        var service = new JwtService(db, _configuration);
        var login = new LoginDTO { Email = "notfound@email.com", Password = "secret123" };

        var result = await service.FindUserWithLogin(login);

        Assert.Null(result);
    }

    [Fact]
    public async Task FindUserWithLogin_EmailIsCaseInsensitive_ReturnsUser()
    {
        var db = _inMemoryDb.GetInMemory();
        await TestDataSeeder.SeedUserAsync(db);
        var service = new JwtService(db, _configuration);
        var login = new LoginDTO { Email = "TEST@EMAIL.COM", Password = "secret123" };

        var result = await service.FindUserWithLogin(login);

        Assert.NotNull(result);
    }

    [Fact]
    public async Task JWTGenerator_ValidUser_ReturnsNonEmptyToken()
    {
        var db = _inMemoryDb.GetInMemory();
        var user = await TestDataSeeder.SeedUserAsync(db);
        var service = new JwtService(db, _configuration);

        var token = service.JWTGenerator(user);

        Assert.NotNull(token);
        Assert.NotEmpty(token);
    }

    [Fact]
    public async Task ValidateToken_ValidToken_ReturnsPrincipal()
    {
        var db = _inMemoryDb.GetInMemory();
        var user = await TestDataSeeder.SeedUserAsync(db);
        var service = new JwtService(db, _configuration);
        var token = service.JWTGenerator(user);

        var principal = service.ValidateToken(token);

        Assert.NotNull(principal);
        Assert.Equal("test@email.com", principal.FindFirstValue(ClaimTypes.Email));
    }

    [Fact]
    public void ValidateToken_ExpiredToken_ReturnsNull()
    {
        var db = _inMemoryDb.GetInMemory();
        var service = new JwtService(db, _configuration);
        var expiredToken = BuildToken("ThisIsASecretKeyForTestingPurposes123!", DateTime.UtcNow.AddMinutes(-10));

        var principal = service.ValidateToken(expiredToken);

        Assert.Null(principal);
    }

    [Fact]
    public async Task ValidateToken_TamperedSignature_ReturnsNull()
    {
        var db = _inMemoryDb.GetInMemory();
        var user = await TestDataSeeder.SeedUserAsync(db);
        var service = new JwtService(db, _configuration);
        var token = service.JWTGenerator(user);

        // Tamper: flip the last character of the signature segment
        var parts = token.Split('.');
        var tamperedSignature = parts[2][..^1] + (parts[2][^1] == 'A' ? 'B' : 'A');
        var tamperedToken = $"{parts[0]}.{parts[1]}.{tamperedSignature}";

        var principal = service.ValidateToken(tamperedToken);

        Assert.Null(principal);
    }

    [Fact]
    public void ValidateToken_SignedWithWrongKey_ReturnsNull()
    {
        var db = _inMemoryDb.GetInMemory();
        var service = new JwtService(db, _configuration);
        var tokenFromWrongKey = BuildToken("ADifferentSecretKeyThatDoesNotMatch456!", DateTime.UtcNow.AddMinutes(30));

        var principal = service.ValidateToken(tokenFromWrongKey);

        Assert.Null(principal);
    }

    [Fact]
    public void ValidateToken_MalformedToken_ReturnsNull()
    {
        var db = _inMemoryDb.GetInMemory();
        var service = new JwtService(db, _configuration);

        var principal = service.ValidateToken("this.is.not-a-valid-jwt");

        Assert.Null(principal);
    }

    [Fact]
    public async Task GenerateRefreshToken_ValidUser_PersistsAndReturnsToken()
    {
        var db = _inMemoryDb.GetInMemory();
        var user = await TestDataSeeder.SeedUserAsync(db);
        var service = new JwtService(db, _configuration);

        var refreshToken = await service.GenerateRefreshToken(user.Id);

        Assert.NotNull(refreshToken);
        Assert.NotEmpty(refreshToken.Token);
        Assert.Equal(user.Id, refreshToken.UserId);
        Assert.True(refreshToken.IsActive);
        Assert.Single(db.RefreshTokens);
    }

    [Fact]
    public async Task RefreshAccessToken_ValidRefreshToken_ReturnsNewAccessAndRefreshTokens()
    {
        var db = _inMemoryDb.GetInMemory();
        var user = await TestDataSeeder.SeedUserAsync(db);
        var service = new JwtService(db, _configuration);
        var originalRefreshToken = await service.GenerateRefreshToken(user.Id);

        var result = await service.RefreshAccessToken(originalRefreshToken.Token);

        Assert.NotNull(result);
        Assert.NotEmpty(result.AccessToken);
        Assert.NotEmpty(result.RefreshToken);
        Assert.NotEqual(originalRefreshToken.Token, result.RefreshToken);

        // New access token should validate correctly
        var principal = service.ValidateToken(result.AccessToken);
        Assert.NotNull(principal);
    }

    [Fact]
    public async Task RefreshAccessToken_RotatesOldToken_OldTokenIsRevoked()
    {
        var db = _inMemoryDb.GetInMemory();
        var user = await TestDataSeeder.SeedUserAsync(db);
        var service = new JwtService(db, _configuration);
        var originalRefreshToken = await service.GenerateRefreshToken(user.Id);

        await service.RefreshAccessToken(originalRefreshToken.Token);

        var storedOldToken = db.RefreshTokens.First(rt => rt.Token == originalRefreshToken.Token);
        Assert.True(storedOldToken.IsRevoked);
        Assert.False(storedOldToken.IsActive);
    }

    [Fact]
    public async Task RefreshAccessToken_UsedTwice_SecondCallThrowsInvalidRefreshTokenException()
    {
        var db = _inMemoryDb.GetInMemory();
        var user = await TestDataSeeder.SeedUserAsync(db);
        var service = new JwtService(db, _configuration);
        var originalRefreshToken = await service.GenerateRefreshToken(user.Id);

        // First use succeeds
        await service.RefreshAccessToken(originalRefreshToken.Token);

        // Reusing the same, refresh token must fail
        await Assert.ThrowsAsync<InvalidRefreshTokenException>(
            () => service.RefreshAccessToken(originalRefreshToken.Token));
    }

    [Fact]
    public async Task RefreshAccessToken_UnknownToken_ThrowsInvalidRefreshTokenException()
    {
        var db = _inMemoryDb.GetInMemory();
        await TestDataSeeder.SeedUserAsync(db);
        var service = new JwtService(db, _configuration);

        await Assert.ThrowsAsync<InvalidRefreshTokenException>(
            () => service.RefreshAccessToken("a-token-that-was-never-issued"));
    }

    [Fact]
    public async Task RefreshAccessToken_ExpiredRefreshToken_ThrowsInvalidRefreshTokenException()
    {
        var db = _inMemoryDb.GetInMemory();
        var user = await TestDataSeeder.SeedUserAsync(db);
        var service = new JwtService(db, _configuration);

        var expiredRefreshToken = new RefreshToken
        {
            Token = "expired-refresh-token-value",
            UserId = user.Id,
            ExpiresAt = DateTime.UtcNow.AddDays(-1)
        };
        db.RefreshTokens.Add(expiredRefreshToken);
        await db.SaveChangesAsync();

        await Assert.ThrowsAsync<InvalidRefreshTokenException>(
            () => service.RefreshAccessToken(expiredRefreshToken.Token));
    }
}