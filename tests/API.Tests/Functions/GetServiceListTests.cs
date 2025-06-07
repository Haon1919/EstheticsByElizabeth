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
    public class GetServiceListTests
    {
        private readonly DbContextOptions<ProjectContext> _dbContextOptions;
        private readonly Mock<ILogger<GetServiceList>> _loggerMock;

        public GetServiceListTests()
        {
            // Set up an in-memory database for testing
            _dbContextOptions = new DbContextOptionsBuilder<ProjectContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
            
            // Set up logger mock
            _loggerMock = new Mock<ILogger<GetServiceList>>();
        }

        private ProjectContext CreateContext()
        {
            return new ProjectContext(_dbContextOptions);
        }

        private async Task SeedDatabase()
        {
            using var context = CreateContext();
            
            // Add categories
            var facialCategory = new Category { Id = 1, Name = "Facial Treatments" };
            var bodyCategory = new Category { Id = 2, Name = "Body Treatments" };
            
            context.Categories.AddRange(facialCategory, bodyCategory);
            await context.SaveChangesAsync();

            // Add services
            var services = new[]
            {
                new Service
                {
                    Id = 101,
                    Name = "Classic Facial",
                    Description = "A relaxing facial treatment",
                    Price = 75.00m,
                    Duration = 60,
                    CategoryId = 1
                },
                new Service
                {
                    Id = 102,
                    Name = "Deep Cleansing Facial",
                    Description = "Deep pore cleansing treatment",
                    Price = 95.00m,
                    Duration = 90,
                    CategoryId = 1
                },
                new Service
                {
                    Id = 201,
                    Name = "Full Body Massage",
                    Description = "Relaxing full body massage",
                    Price = 120.00m,
                    Duration = 90,
                    CategoryId = 2
                }
            };

            context.Services.AddRange(services);
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task Run_ReturnsAllServices_WhenServicesExist()
        {
            // Arrange
            await SeedDatabase();
            var function = new GetServiceList(_loggerMock.Object, CreateContext());
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
            var services = Assert.IsAssignableFrom<System.Collections.IEnumerable>(okResult.Value);
            
            // Convert to list to count items
            var serviceList = services.Cast<object>().ToList();
            Assert.Equal(3, serviceList.Count);
        }

        [Fact]
        public async Task Run_ReturnsEmptyList_WhenNoServicesExist()
        {
            // Arrange
            var function = new GetServiceList(_loggerMock.Object, CreateContext());
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
            var services = Assert.IsAssignableFrom<System.Collections.IEnumerable>(okResult.Value);
            
            // Convert to list to count items
            var serviceList = services.Cast<object>().ToList();
            Assert.Empty(serviceList);
        }

        [Fact]
        public async Task Run_HandlesCorsPreflight_ReturnsOkResult()
        {
            // Arrange
            var function = new GetServiceList(_loggerMock.Object, CreateContext());
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
            var function = new GetServiceList(_loggerMock.Object, CreateContext());
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
        public async Task Run_ReturnsServicesOrderedByCategory_ThenByName()
        {
            // Arrange
            await SeedDatabase();
            var function = new GetServiceList(_loggerMock.Object, CreateContext());
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
            var services = okResult.Value as dynamic[];
            
            // Verify services are ordered correctly (Body comes before Facial alphabetically)
            Assert.NotNull(services);
            Assert.Equal(3, services.Length);
            
            // First service should be from Body Treatments category
            dynamic firstService = services[0];
            Assert.Equal("Body Treatments", firstService.Category.Name.ToString());
        }

        [Fact]
        public async Task Run_HandlesException_ReturnsInternalServerError()
        {
            // Arrange
            var mockContext = new Mock<ProjectContext>(new DbContextOptionsBuilder<ProjectContext>()
                .UseInMemoryDatabase($"ErrorTestDb_{Guid.NewGuid()}")
                .Options);
            
            // Setup the mock to throw an exception
            mockContext.Setup(c => c.Services).Throws(new Exception("Database error"));
            
            var function = new GetServiceList(_loggerMock.Object, mockContext.Object);
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
