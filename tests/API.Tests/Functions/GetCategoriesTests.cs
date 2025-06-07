using System;
using System.Threading.Tasks;
using API.Data;
using API.Entities;
using API.Functions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace API.Tests.Functions
{
    public class GetCategoriesTests
    {
        private readonly DbContextOptions<ProjectContext> _dbContextOptions;
        private readonly Mock<ILogger<GetCategories>> _loggerMock;

        public GetCategoriesTests()
        {
            // Set up an in-memory database for testing
            _dbContextOptions = new DbContextOptionsBuilder<ProjectContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
            
            // Set up logger mock
            _loggerMock = new Mock<ILogger<GetCategories>>();
        }

        private ProjectContext CreateContext()
        {
            return new ProjectContext(_dbContextOptions);
        }

        private async Task SeedDatabase()
        {
            using var context = CreateContext();
            
            // Add categories
            var categories = new[]
            {
                new Category { Id = 1, Name = "Facial Treatments" },
                new Category { Id = 2, Name = "Body Treatments" },
                new Category { Id = 3, Name = "Hair Services" }
            };
            
            context.Categories.AddRange(categories);
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task Run_ReturnsAllCategories_WhenCategoriesExist()
        {
            // Arrange
            await SeedDatabase();
            var function = new GetCategories(_loggerMock.Object, CreateContext());
            var mockRequest = new Mock<HttpRequest>();
            var mockContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaders = new HeaderDictionary();

            mockResponse.Setup(r => r.Headers).Returns(mockHeaders);
            mockContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockContext.Object);
            mockRequest.Setup(r => r.Method).Returns("GET");

            // Act
            var result = await function.Run(mockRequest.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var categories = Assert.IsAssignableFrom<System.Collections.IEnumerable>(okResult.Value);
            
            // Convert to list to count items
            var categoryList = categories.Cast<object>().ToList();
            Assert.Equal(3, categoryList.Count);
        }

        [Fact]
        public async Task Run_ReturnsEmptyList_WhenNoCategoriesExist()
        {
            // Arrange
            var function = new GetCategories(_loggerMock.Object, CreateContext());
            var mockRequest = new Mock<HttpRequest>();
            var mockContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaders = new HeaderDictionary();

            mockResponse.Setup(r => r.Headers).Returns(mockHeaders);
            mockContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockContext.Object);
            mockRequest.Setup(r => r.Method).Returns("GET");

            // Act
            var result = await function.Run(mockRequest.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var categories = Assert.IsAssignableFrom<System.Collections.IEnumerable>(okResult.Value);
            
            // Convert to list to count items
            var categoryList = categories.Cast<object>().ToList();
            Assert.Empty(categoryList);
        }

        [Fact]
        public async Task Run_HandlesCorsPreflight_ReturnsOkResult()
        {
            // Arrange
            var function = new GetCategories(_loggerMock.Object, CreateContext());
            var mockRequest = new Mock<HttpRequest>();
            var mockContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaders = new HeaderDictionary();

            mockResponse.Setup(r => r.Headers).Returns(mockHeaders);
            mockContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockContext.Object);
            mockRequest.Setup(r => r.Method).Returns("OPTIONS");

            // Act
            var result = await function.Run(mockRequest.Object);

            // Assert
            Assert.IsType<OkResult>(result);
            Assert.True(mockHeaders.ContainsKey("Access-Control-Allow-Origin"));
            Assert.True(mockHeaders.ContainsKey("Access-Control-Allow-Methods"));
        }

        [Fact]
        public async Task Run_SetsCorrectCorsHeaders_ForGetRequest()
        {
            // Arrange
            await SeedDatabase();
            var function = new GetCategories(_loggerMock.Object, CreateContext());
            var mockRequest = new Mock<HttpRequest>();
            var mockContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaders = new HeaderDictionary();

            mockResponse.Setup(r => r.Headers).Returns(mockHeaders);
            mockContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockContext.Object);
            mockRequest.Setup(r => r.Method).Returns("GET");

            // Act
            var result = await function.Run(mockRequest.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.True(mockHeaders.ContainsKey("Access-Control-Allow-Origin"));
            Assert.Equal("*", mockHeaders["Access-Control-Allow-Origin"].ToString());
        }

        [Fact]
        public async Task Run_ReturnsCategoriesOrderedByName()
        {
            // Arrange
            await SeedDatabase();
            var function = new GetCategories(_loggerMock.Object, CreateContext());
            var mockRequest = new Mock<HttpRequest>();
            var mockContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaders = new HeaderDictionary();

            mockResponse.Setup(r => r.Headers).Returns(mockHeaders);
            mockContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockContext.Object);
            mockRequest.Setup(r => r.Method).Returns("GET");

            // Act
            var result = await function.Run(mockRequest.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var categories = okResult.Value as dynamic[];
            
            Assert.NotNull(categories);
            Assert.Equal(3, categories.Length);
            
            // Verify categories are ordered alphabetically
            dynamic firstCategory = categories[0];
            dynamic secondCategory = categories[1];
            dynamic thirdCategory = categories[2];
            
            Assert.Equal("Body Treatments", firstCategory.Name.ToString());
            Assert.Equal("Facial Treatments", secondCategory.Name.ToString());
            Assert.Equal("Hair Services", thirdCategory.Name.ToString());
        }

        [Fact]
        public async Task Run_HandlesException_ReturnsInternalServerError()
        {
            // Arrange
            var mockContext = new Mock<ProjectContext>(new DbContextOptionsBuilder<ProjectContext>()
                .UseInMemoryDatabase($"ErrorTestDb_{Guid.NewGuid()}")
                .Options);
            
            // Setup the mock to throw an exception
            mockContext.Setup(c => c.Categories).Throws(new Exception("Database error"));
            
            var function = new GetCategories(_loggerMock.Object, mockContext.Object);
            var mockRequest = new Mock<HttpRequest>();
            var mockHttpContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaders = new HeaderDictionary();

            mockResponse.Setup(r => r.Headers).Returns(mockHeaders);
            mockHttpContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockHttpContext.Object);
            mockRequest.Setup(r => r.Method).Returns("GET");

            // Act
            var result = await function.Run(mockRequest.Object);

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
        }
    }
}
