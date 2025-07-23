using API_Shopping.Interfaces;
using API_Shopping.Controllers;
using NSubstitute;
using Microsoft.AspNetCore.Mvc;
using API_Shopping.Models;

namespace Shopping.Tests
{
    public class ProductControllerTests
    {
        private readonly IProductService _productService;
        private readonly ProductsController _productController;

        public ProductControllerTests() 
        {
            _productService = Substitute.For<IProductService>();
            _productController = new ProductsController(_productService);
        }

        /*
         * Try to add product successfully with product complete
         * Result: Add product to database
         */
        [Fact]
        public async Task AddProductSuccesfully() 
        {
            //Arrage
            var newProduct = new ProductCreateDTO
            {
                Name = "product",
                Description = "product description",
                Category = "product category",
                CodeCABYS = "code CABYS", 
                Price = 9999, 
                Quantity = 9999
            };

            var created = new Product
            {
                Id = 1,
                Name = newProduct.Name,
                Description = newProduct.Description,
                Category = newProduct.Category,
                Quantity = newProduct.Quantity,
                Price = newProduct.Price,
                CodeCABYS = newProduct.CodeCABYS
            };

            _productService.AddProduct(newProduct).Returns(Task.FromResult(created));

            //Act
            var result = await _productController.AddProduct(newProduct);

            //Assert
            var createdResult = Assert.IsType<CreatedAtActionResult>(result.Result);
            Assert.Equal("GetProduct", createdResult.ActionName);

            var returnProduct = Assert.IsType<Product>(createdResult.Value);
            Assert.Equal(newProduct.Name, returnProduct.Name);
            Assert.Equal(newProduct.Description, returnProduct.Description);
            Assert.Equal(newProduct.Price, returnProduct.Price);
            Assert.Equal(newProduct.Quantity, returnProduct.Quantity);
        }

        /*
         * Try to add product successfully with a null product
         * Result: Dont add the product in the database and returns a 400 error
         */
        [Fact]
        public async Task AddProductWithServiceNull()
        {
            //Arrage
            var productNull =  _productService.AddProduct(null).Returns((Product)null);

            //Act 
            var result = await _productController.AddProduct(null);

            //Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            Assert.Equal("No se pudo crear el producto. Intente mas tarde.", badRequest.Value);
        }

        /*
         * Try to add product successfully with a product without name
         * Result: Dont add the product in the database and returns a 400 error
         */
        [Fact]
        public async Task AddProduct_ValidationNameRequired()
        {
            //Arrage
            var product = new ProductCreateDTO
            {
                Name = "", 
                Description = "Description",
                Category = "Category", 
                CodeCABYS = "123456789", 
                Price = 999, 
                Quantity = 999
            };

            _productController.ModelState.AddModelError("Name", "The Name field is required.");

            //Act
            var result = await _productController.AddProduct(product);

            //Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            var error = badRequest.Value as SerializableError;
            Assert.NotNull(error);
            Assert.True(error.ContainsKey("Name"));
        }

        /*
         * Try to add product successfully with a product without description
         * Result: Dont add the product in the database and returns a 400 error
         */
        [Fact]
        public async Task AddProduct_ValidationDescriptionRequired() {
            //Arrage
            var product = new ProductCreateDTO
            {
                Name = "Name",
                Description = "",
                Category = "Category",
                CodeCABYS = "123456789",
                Price = 999,
                Quantity = 999
            };
 
            _productController.ModelState.AddModelError("Description", "The Description field is required.");

            //Act
            var result = await _productController.AddProduct(product);

            //Assert 
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            var error = badRequest.Value as SerializableError;
            Assert.NotNull(error);
            Assert.True(error.ContainsKey("Description"));
        }

        /*
         * Try to add product successfully with a product without category
         * Result: Dont add the product in the database and returns a 400 error
         */
        [Fact]
        public async Task AddProduct_ValidationCategoryRequired()
        {
            //Arrage
            var product = new ProductCreateDTO
            {
                Name = "Name",
                Description = "Description",
                Category = "",
                CodeCABYS = "123456789",
                Price = 999,
                Quantity = 999
            };

            _productController.ModelState.AddModelError("Category", "The Category field is required.");

            //Act 

            var result = await _productController.AddProduct(product);

            //Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            var error = badRequest.Value as SerializableError;
            Assert.NotNull(error);
            Assert.True(error.ContainsKey("Category"));
        }

        /*
         * Try to add product successfully with a product without a CABYS code
         * Result: Dont add the product in the database and returns a 400 error
         */
        [Fact]
        public async Task AddProduct_ValidationCodeCABYSRequired()
        {
            //Arrage
            var product = new ProductCreateDTO
            {
                Name = "Name",
                Description = "Description",
                Category = "Category",
                CodeCABYS = "",
                Price = 999,
                Quantity = 999
            };

            _productController.ModelState.AddModelError("CodeCABYS", "The CodeCABYS field is required.");

            //Act 
            var result = await _productController.AddProduct(product);

            //Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            var error = badRequest.Value as SerializableError;
            Assert.NotNull(error);
            Assert.True(error.ContainsKey("CodeCABYS"));
        }

        /*
         * Try to add product successfully with a product that the price is zero
         * Result: Dont add the product in the database and returns a 400 error
         */
        [Fact]
        public async Task AddProduct_ValidationPriceEqualsZero()
        {
            //Arrage
            var product = new ProductCreateDTO
            {
                Name = "Name",
                Description = "Description",
                Category = "Category",
                CodeCABYS = "CodeCABYS",
                Price = 0,
                Quantity = 999
            };
            _productController.ModelState.AddModelError("Price", "The Price field must be between 1 and 99999");

            //Act

            var result = await _productController.AddProduct(product);

            //Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            var error = badRequest.Value as SerializableError;
            Assert.NotNull(error);
            Assert.True(error.ContainsKey("Price"));
        }

        /*
         * Try to add product successfully with a product that the price is less than zero
         * Result: Dont add the product in the database and returns a 400 error
         */
        [Fact]
        public async Task AddProduct_ValidationPriceLessThanZero()
        {
            //Arrage
            var product = new ProductCreateDTO
            {
                Name = "Name",
                Description = "Description",
                Category = "Category",
                CodeCABYS = "CodeCABYS",
                Price = -999,
                Quantity = 999
            };

            _productController.ModelState.AddModelError("Price", "The Price field must be between 1 and 99999");

            //Act
            var result = await _productController.AddProduct(product);

            //Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            var error = badRequest.Value as SerializableError;
            Assert.NotNull(error);
            Assert.True(error.ContainsKey("Price"));
        }

        /*
         * Try to add product successfully with a product that the quantity is zero
         * Result: Dont add the product in the database and returns a 400 error
         */
        [Fact]
        public async Task AddProduct_ValidationQuantityEqualsZero()
        {
            //Arrage
            var product = new ProductCreateDTO
            {
                Name = "Name",
                Description = "Description",
                Category = "Category",
                CodeCABYS = "CodeCABYS",
                Price = 999, 
                Quantity = 0
            };

            _productController.ModelState.AddModelError("Quantity", "The quantity field must be between 1 and 99999");

            //Act 
            var result = await _productController.AddProduct(product);

            //Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            var error = badRequest?.Value as SerializableError;
            Assert.NotNull(error);
            Assert.True(error.ContainsKey("Quantity"));
        }

        /*
         * Try to add product successfully with a product that the quantity is less than zero
         * Result: Dont add the product in the database and returns a 400 error
         */
        [Fact]
        public async Task AddProduct_ValidationQuantityLessThanZero()
        {
            //Arrage
            var product = new ProductCreateDTO
            {
                Name = "Name",
                Description = "Description",
                Category = "Category",
                CodeCABYS = "CodeCABYS",
                Price = 999,
                Quantity = -999
            };

            _productController.ModelState.AddModelError("Quantity", "The quantity field must be between 1 and 99999");

            //Act
            var result = await _productController.AddProduct(product);

            //Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            var error = badRequest.Value as SerializableError;
            Assert.NotNull(error);
            Assert.True(error.ContainsKey("Quantity"));
        }

        /*
         * Try to add product successfully with a product that the category contains incorrect characters
         * Result: Dont add the product in the database and returns a 400 error
         */
        [Fact]
        public async Task AddProduct_ValidationCategoryWithIncorrectCharacters() 
        {
            //Arrage
            var product = new ProductCreateDTO
            {
                Name = "Name",
                Description = "Description",
                Category = "Cate2gory",
                CodeCABYS = "CodeCABYS",
                Price = 999,
                Quantity = -999
            };

            _productController.ModelState.AddModelError("Category", "The field must contains only leters.");

            //Act 
            var result = await _productController.AddProduct(product);

            //Assert
            var badRequest = Assert.IsType<BadRequestObjectResult>(result.Result);
            var error = badRequest?.Value as SerializableError;
            Assert.NotNull(error);
            Assert.True(error.ContainsKey("Category"));
        }
    }
}
