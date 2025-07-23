namespace Shopping.Tests;

using API_Shopping.Models;
using API_Shopping.Services;

public class ProductServiceTest
{
    private readonly InMemoryDb _inMemoryDb;

    public ProductServiceTest()
    {
        _inMemoryDb = new InMemoryDb();
    }

    /*
     * Try to add product successfully with product complete
     * Result: Add product in database
     */
    [Fact]
    public async Task AddProductSuccessfull()
    {
        //Arrage
        var database = _inMemoryDb.GetInMemory();
        var service = new ProductService(database);
        var productDTO = new ProductCreateDTO
        {
            Name = "ProductTest",
            Description = "DescriptionTest",
            Category = "CategoryTest", 
            CodeCABYS = "CodeCABYSTest", 
            Price = 9999, 
            Quantity = 9999
        };

        //Act
        var product = await service.AddProduct(productDTO);

        //Assert
        Assert.NotNull(product);
        Assert.Equal("ProductTest", product.Name);
        Assert.Single(database.Products);
    }

    /*
     * Try to add product successfully with null 
     * Result: Don't add product un database
     */
    [Fact]
    public async Task AddProductNull() 
    { 
        //Arrage
        var db = _inMemoryDb.GetInMemory();
        var service = new ProductService(db);

        //Act
        var product = await service.AddProduct(null);

        //Assert
        Assert.Null(product);
        Assert.Empty(db.Products);
    }
}
