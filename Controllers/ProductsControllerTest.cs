using API_Shopping.Controllers;
using API_Shopping.DTOs.Product;
using API_Shopping.Models;
using API_Shopping.Services;
using Microsoft.AspNetCore.Mvc;
using Shopping.Tests.Helpers;

namespace Shopping.Tests.Controllers;

public class ProductsControllerTest
{
    private readonly InMemoryDb _inMemoryDb;

    public ProductsControllerTest()
    {
        _inMemoryDb = new InMemoryDb();
    }

    [Fact]
    public async Task AddProduct_ValidProduct_ReturnsCreatedAtAction()
    {
        var db = _inMemoryDb.GetInMemory();
        var service = new ProductService(db);
        var controller = new ProductsController(service);

        var product = new ProductCreateDTO
        {
            Name = "Laptop",
            Description = "Gaming laptop",
            Price = 1500,
            CodeCabys = "Code_Cabys",
            DescriptionCabys = "Description_Cabys",
            TaxCabys = 13,
            Category = "Technology"
        };

        var result = await controller.AddProduct(product);

        var created = Assert.IsType<CreatedAtActionResult>(result.Result);

        Assert.Equal("GetProduct", created.ActionName);
        Assert.NotNull(created.Value);
    }

    [Fact]
    public async Task GetProduct_ExistingId_ReturnsOk()
    {
        var db = _inMemoryDb.GetInMemory();

        var service = new ProductService(db);

        var created = await TestDataSeeder.SeedProductAsync(db);

        var controller = new ProductsController(service);

        var result = await controller.GetProduct(created.Id);

        var ok = Assert.IsType<OkObjectResult>(result.Result);

        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task GetProducts_ReturnsOk()
    {
        var db = _inMemoryDb.GetInMemory();

        var service = new ProductService(db);

        var controller = new ProductsController(service);

        var result = await controller.GetProducts();

        var ok = Assert.IsType<OkObjectResult>(result);

        Assert.NotNull(ok.Value);
    }

    [Fact]
    public async Task PutProduct_ValidData_ReturnsNoContent()
    {
        var db = _inMemoryDb.GetInMemory();

        var service = new ProductService(db);

        var product = await TestDataSeeder.SeedProductAsync(db);

        var controller = new ProductsController(service);

        var updateDto = new ProductUpdateDTO
        {
            Id = product.Id,
            Name = "Keyboard Pro",
            Description = "Mechanical RGB",
            Price = 70,
            CodeCabys = "Code_Cabys",
            DescriptionCabys = "Description_Cabys",
            TaxCabys = 13,
            Category = "Accessories"
        };

        var result = await controller.PutProduct(product.Id, updateDto);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task PutProduct_DifferentIds_ReturnsBadRequest()
    {
        var db = _inMemoryDb.GetInMemory();

        var service = new ProductService(db);

        var controller = new ProductsController(service);

        var dto = new ProductUpdateDTO
        {
            Id = 99
        };

        var result = await controller.PutProduct(1, dto);

        var badRequest = Assert.IsType<BadRequestObjectResult>(result);

        Assert.Equal(
            "The ID in the URL does not match the ID in the request body.",
            badRequest.Value);
    }

    [Fact]
    public async Task DisableProduct_ReturnsNoContent()
    {
        var db = _inMemoryDb.GetInMemory();

        var service = new ProductService(db);

        var product = await TestDataSeeder.SeedProductAsync(db);

        var controller = new ProductsController(service);

        var result = await controller.DisableProduct(product.Id);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task EnableProduct_ReturnsNoContent()
    {
        var db = _inMemoryDb.GetInMemory();

        var service = new ProductService(db);

        var product = await TestDataSeeder.SeedProductAsync(db);

        await service.DisableProduct(product.Id);

        var controller = new ProductsController(service);

        var result = await controller.EnableProduct(product.Id);

        Assert.IsType<NoContentResult>(result);
    }

    [Fact]
    public async Task GetShowProducts_ReturnsOk()
    {
        var db = _inMemoryDb.GetInMemory();

        var service = new ProductService(db);

        var controller = new ProductsController(service);

        var result = await controller.GetShowProducts();

        var ok = Assert.IsType<OkObjectResult>(result.Result);

        Assert.NotNull(ok.Value);
    }
}