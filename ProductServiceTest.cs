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
        var product = service.AddProduct(productDTO);

        Assert.NotNull(product);
        Assert.Equal("ProductTest", product.Result.Name);
        Assert.Single(database.Products);
    }
}
