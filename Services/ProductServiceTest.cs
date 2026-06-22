using API_Shopping.DTOs.Product;
using API_Shopping.Exceptions.Product;
using API_Shopping.Services;
using Shopping.Tests.Helpers;

namespace Shopping.Tests.Services;

public class ProductServiceTest
{
    private readonly InMemoryDb _inMemoryDb;

    public ProductServiceTest()
    {
        _inMemoryDb = new InMemoryDb();
    }

    // Helper - Builds a valid ProductCreateDTO
    private static ProductCreateDTO ValidCreateDTO(string name = "New Product") => new()
    {
        Name = name,
        Description = "A valid description",
        Category = "Electronics",
        CodeCabys = "654321",
        DescriptionCabys = "Valid CABYS description",
        Price = 500,
        TaxCabys = 0.13m
    };

    [Fact]
    public async Task AddProduct_ValidDTO_ReturnsPersistedProduct()
    {
        var db = _inMemoryDb.GetInMemory();
        var service = new ProductService(db);

        var result = await service.AddProduct(ValidCreateDTO());

        Assert.NotNull(result);
        Assert.Equal("New Product", result.Name);
        Assert.True(result.IsActive);
        Assert.Single(db.Products);
    }

    [Fact]
    public async Task AddProduct_NullDTO_ThrowsProductCreationException()
    {
        var db = _inMemoryDb.GetInMemory();
        var service = new ProductService(db);

        await Assert.ThrowsAsync<ProductCreationException>(() => service.AddProduct(null!));
    }

    [Fact]
    public async Task AddProduct_SetsIsActiveTrue_AndCreatedAt()
    {
        var db = _inMemoryDb.GetInMemory();
        var service = new ProductService(db);

        var result = await service.AddProduct(ValidCreateDTO());

        Assert.True(result.IsActive);
        Assert.NotNull(result.CreateAt);
    }

    [Fact]
    public async Task GetProductById_ExistingId_ReturnsProduct()
    {
        var db = _inMemoryDb.GetInMemory();
        var product = await TestDataSeeder.SeedProductAsync(db);
        var service = new ProductService(db);

        var result = await service.GetProductById(product.Id);

        Assert.NotNull(result);
        Assert.Equal(product.Id, result.Id);
        Assert.Equal("Test Product", result.Name);
    }

    [Fact]
    public async Task GetProductById_NonExistingId_ThrowsProductNotFoundException()
    {
        var db = _inMemoryDb.GetInMemory();
        var service = new ProductService(db);

        await Assert.ThrowsAsync<ProductNotFoundException>(() => service.GetProductById(999));
    }

    [Fact]
    public async Task GetProducts_ReturnsPagedResult()
    {
        var db = _inMemoryDb.GetInMemory();
        await TestDataSeeder.SeedProductAsync(db);
        var service = new ProductService(db);

        var result = await service.GetProducts(page: 1, pageSize: 10);

        var page = result.GetType().GetProperty("page")?.GetValue(result);
        var data = result.GetType().GetProperty("data")?.GetValue(result) as IEnumerable<object>;

        Assert.Equal(1, page);
        Assert.NotNull(data);
        Assert.Single(data);
    }

    [Fact]
    public async Task GetCatalogProducts_NoCategory_ReturnsAllProducts()
    {
        var db = _inMemoryDb.GetInMemory();
        await TestDataSeeder.SeedProductAsync(db, isActive: true);
        var service = new ProductService(db);

        var result = await service.GetCatalogProducts();

        var data = result.GetType().GetProperty("data")?.GetValue(result) as IEnumerable<object>;
        Assert.NotNull(data);
        Assert.Single(data);
    }

    [Fact]
    public async Task GetCatalogProducts_WithMatchingCategory_ReturnsFilteredProducts()
    {
        var db = _inMemoryDb.GetInMemory();
        await TestDataSeeder.SeedProductAsync(db, id: 1, category: "Electronics", isActive: true);
        await TestDataSeeder.SeedProductAsync(db, id: 2, name: "Other Product", category: "Clothing", isActive: true);
        var service = new ProductService(db);

        var result = await service.GetCatalogProducts(category: "Electronics");

        var data = result.GetType().GetProperty("data")?.GetValue(result) as IEnumerable<object>;
        Assert.NotNull(data);
        Assert.Single(data);
    }

    [Fact]
    public async Task GetCatalogProducts_PriceIncludesTax()
    {
        var db = _inMemoryDb.GetInMemory();
        await TestDataSeeder.SeedProductAsync(db, price: 1000, taxCabys: 0.13m, isActive: true);
        var service = new ProductService(db);

        var result = await service.GetCatalogProducts();

        var data = result.GetType().GetProperty("data")?.GetValue(result) as IEnumerable<ProductResponseDTO>;
        var first = data?.First();
        Assert.Equal(1130m, first?.Price); // 1000 + (1000 * 0.13)
    }

    [Fact]
    public async Task UpdateProduct_ValidId_UpdatesFieldsAndReturnsTrue()
    {
        var db = _inMemoryDb.GetInMemory();
        var product = await TestDataSeeder.SeedProductAsync(db);
        var service = new ProductService(db);
        var updateDto = new ProductUpdateDTO
        {
            Id = product.Id,
            Name = "Updated Name",
            Description = "Updated desc",
            Category = "Updated Category",
            Price = 12345,
            TaxCabys = 0.10m,
            CodeCabys = "NEW123",
            DescriptionCabys = "Updated CABYS"
        };

        var result = await service.UpdateProduct(product.Id, updateDto);

        Assert.True(result);
        var updated = await db.Products.FindAsync(product.Id);
        Assert.Equal("Updated Name", updated!.Name);
        Assert.NotNull(updated.UpdateAt);
    }

    [Fact]
    public async Task UpdateProduct_NonExistingId_ThrowsProductNotFoundException()
    {
        var db = _inMemoryDb.GetInMemory();
        var service = new ProductService(db);
        var updateDto = new ProductUpdateDTO
        {
            Id = 999,
            Name = "X",
            Description = "X",
            Category = "X",
            Price = 1,
            TaxCabys = 0,
            CodeCabys = "X",
            DescriptionCabys = "X"
        };

        await Assert.ThrowsAsync<ProductNotFoundException>(() => service.UpdateProduct(999, updateDto));
    }

    [Fact]
    public async Task DisableProduct_ExistingId_SetsIsActiveFalse()
    {
        var db = _inMemoryDb.GetInMemory();
        var product = await TestDataSeeder.SeedProductAsync(db, isActive: true);
        var service = new ProductService(db);

        var result = await service.DisableProduct(product.Id);

        Assert.True(result);
        var updated = await db.Products.FindAsync(product.Id);
        Assert.False(updated!.IsActive);
        Assert.NotNull(updated.UpdateAt);
    }

    [Fact]
    public async Task DisableProduct_NonExistingId_ThrowsProductNotFoundException()
    {
        var db = _inMemoryDb.GetInMemory();
        var service = new ProductService(db);

        await Assert.ThrowsAsync<ProductNotFoundException>(() => service.DisableProduct(999));
    }

    [Fact]
    public async Task EnableProduct_ExistingId_SetsIsActiveTrue()
    {
        var db = _inMemoryDb.GetInMemory();
        var product = await TestDataSeeder.SeedProductAsync(db, isActive: false);
        var service = new ProductService(db);

        var result = await service.EnableProduct(product.Id);

        Assert.True(result);
        var updated = await db.Products.FindAsync(product.Id);
        Assert.True(updated!.IsActive);
        Assert.NotNull(updated.UpdateAt);
    }

    [Fact]
    public async Task EnableProduct_NonExistingId_ThrowsProductNotFoundException()
    {
        var db = _inMemoryDb.GetInMemory();
        var service = new ProductService(db);

        await Assert.ThrowsAsync<ProductNotFoundException>(() => service.EnableProduct(999));
    }

    [Fact]
    public async Task ProductExists_ExistingId_ReturnsTrue()
    {
        var db = _inMemoryDb.GetInMemory();
        var product = await TestDataSeeder.SeedProductAsync(db);
        var service = new ProductService(db);

        var result = await service.ProductExists(product.Id);

        Assert.True(result);
    }

    [Fact]
    public async Task ProductExists_NonExistingId_ReturnsFalse()
    {
        var db = _inMemoryDb.GetInMemory();
        var service = new ProductService(db);

        var result = await service.ProductExists(999);

        Assert.False(result);
    }
}