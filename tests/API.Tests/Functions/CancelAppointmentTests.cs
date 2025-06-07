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
    public class CancelAppointmentTests
    {
        private readonly DbContextOptions<ProjectContext> _dbContextOptions;
        private readonly Mock<ILogger<CancelAppointment>> _loggerMock;

        public CancelAppointmentTests()
        {
            // Set up an in-memory database for testing
            _dbContextOptions = new DbContextOptionsBuilder<ProjectContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
            
            // Set up logger mock
            _loggerMock = new Mock<ILogger<CancelAppointment>>();
        }

        private ProjectContext CreateContext()
        {
            return new ProjectContext(_dbContextOptions);
        }

        private async Task SeedDatabase()
        {
            using var context = CreateContext();
            
            // Add categories and services
            var category = new Category { Id = 1, Name = "Facial Treatments" };
            context.Categories.Add(category);
            
            var service = new Service
            {
                Id = 101,
                Name = "Classic Facial",
                Description = "A relaxing facial treatment",
                Price = 75.00m,
                Duration = 60,
                CategoryId = 1
            };
            context.Services.Add(service);

            // Add client
            var client = new Client
            {
                Id = 1,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                PhoneNumber = "555-123-4567"
            };
            
            context.Clients.Add(client);
            await context.SaveChangesAsync();

            // Add appointment
            var appointment = new Appointment
            {
                Id = 123,
                ClientId = 1,
                ServiceId = 101,
                Time = DateTimeOffset.UtcNow.AddDays(7) // Future appointment
            };

            context.Appointments.Add(appointment);
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task Run_CancelsAppointment_WhenValidIdProvided()
        {
            // Arrange
            await SeedDatabase();
            var function = new CancelAppointment(_loggerMock.Object, CreateContext());
            var mockRequest = new Mock<HttpRequest>();
            var mockContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaders = new HeaderDictionary();

            mockResponse.Setup(r => r.Headers).Returns(mockHeaders);
            mockContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockContext.Object);
            mockRequest.Setup(r => r.Method).Returns("DELETE");

            // Act
            var result = await function.Run(mockRequest.Object, "123");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value as dynamic;
            
            Assert.NotNull(response);
            Assert.Equal("Appointment cancelled successfully", response.Message.ToString());
            Assert.Equal(123, (int)response.Details.Id);
            
            // Verify appointment was actually deleted from database
            using var verifyContext = CreateContext();
            var deletedAppointment = await verifyContext.Appointments.FindAsync(123);
            Assert.Null(deletedAppointment);
        }

        [Fact]
        public async Task Run_ReturnsNotFound_WhenAppointmentDoesNotExist()
        {
            // Arrange
            await SeedDatabase();
            var function = new CancelAppointment(_loggerMock.Object, CreateContext());
            var mockRequest = new Mock<HttpRequest>();
            var mockContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaders = new HeaderDictionary();

            mockResponse.Setup(r => r.Headers).Returns(mockHeaders);
            mockContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockContext.Object);
            mockRequest.Setup(r => r.Method).Returns("DELETE");

            // Act
            var result = await function.Run(mockRequest.Object, "999");

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("No appointment found with ID: 999", notFoundResult.Value.ToString());
        }

        [Fact]
        public async Task Run_ReturnsBadRequest_WhenIdIsInvalid()
        {
            // Arrange
            var function = new CancelAppointment(_loggerMock.Object, CreateContext());
            var mockRequest = new Mock<HttpRequest>();
            var mockContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaders = new HeaderDictionary();

            mockResponse.Setup(r => r.Headers).Returns(mockHeaders);
            mockContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockContext.Object);
            mockRequest.Setup(r => r.Method).Returns("DELETE");

            // Act
            var result = await function.Run(mockRequest.Object, "invalid-id");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("valid appointment ID", badRequestResult.Value.ToString());
        }

        [Fact]
        public async Task Run_ReturnsBadRequest_WhenIdIsNull()
        {
            // Arrange
            var function = new CancelAppointment(_loggerMock.Object, CreateContext());
            var mockRequest = new Mock<HttpRequest>();
            var mockContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaders = new HeaderDictionary();

            mockResponse.Setup(r => r.Headers).Returns(mockHeaders);
            mockContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockContext.Object);
            mockRequest.Setup(r => r.Method).Returns("DELETE");

            // Act
            var result = await function.Run(mockRequest.Object, null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("valid appointment ID", badRequestResult.Value.ToString());
        }

        [Fact]
        public async Task Run_ReturnsBadRequest_WhenIdIsEmpty()
        {
            // Arrange
            var function = new CancelAppointment(_loggerMock.Object, CreateContext());
            var mockRequest = new Mock<HttpRequest>();
            var mockContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaders = new HeaderDictionary();

            mockResponse.Setup(r => r.Headers).Returns(mockHeaders);
            mockContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockContext.Object);
            mockRequest.Setup(r => r.Method).Returns("DELETE");

            // Act
            var result = await function.Run(mockRequest.Object, "");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("valid appointment ID", badRequestResult.Value.ToString());
        }

        [Fact]
        public async Task Run_HandlesCorsPreflight_ReturnsOkResult()
        {
            // Arrange
            var function = new CancelAppointment(_loggerMock.Object, CreateContext());
            var mockRequest = new Mock<HttpRequest>();
            var mockContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaders = new HeaderDictionary();

            mockResponse.Setup(r => r.Headers).Returns(mockHeaders);
            mockContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockContext.Object);
            mockRequest.Setup(r => r.Method).Returns("OPTIONS");

            // Act
            var result = await function.Run(mockRequest.Object, "123");

            // Assert
            Assert.IsType<OkResult>(result);
            Assert.True(mockHeaders.ContainsKey("Access-Control-Allow-Origin"));
            Assert.True(mockHeaders.ContainsKey("Access-Control-Allow-Methods"));
            Assert.Equal("DELETE, OPTIONS", mockHeaders["Access-Control-Allow-Methods"].ToString());
        }

        [Fact]
        public async Task Run_IncludesClientAndServiceInfoInResponse()
        {
            // Arrange
            await SeedDatabase();
            var function = new CancelAppointment(_loggerMock.Object, CreateContext());
            var mockRequest = new Mock<HttpRequest>();
            var mockContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaders = new HeaderDictionary();

            mockResponse.Setup(r => r.Headers).Returns(mockHeaders);
            mockContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockContext.Object);
            mockRequest.Setup(r => r.Method).Returns("DELETE");

            // Act
            var result = await function.Run(mockRequest.Object, "123");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value as dynamic;
            
            Assert.NotNull(response);
            Assert.NotNull(response.Details);
            
            // Verify client information is included
            Assert.Equal("John Doe", response.Details.Client.ToString());
            
            // Verify service information is included
            Assert.Equal("Classic Facial", response.Details.Service.ToString());
            
            // Verify time information is included
            Assert.NotNull(response.Details.Time);
        }

        [Fact]
        public async Task Run_SetsCorrectCorsHeaders()
        {
            // Arrange
            await SeedDatabase();
            var function = new CancelAppointment(_loggerMock.Object, CreateContext());
            var mockRequest = new Mock<HttpRequest>();
            var mockContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaders = new HeaderDictionary();

            mockResponse.Setup(r => r.Headers).Returns(mockHeaders);
            mockContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockContext.Object);
            mockRequest.Setup(r => r.Method).Returns("DELETE");

            // Act
            var result = await function.Run(mockRequest.Object, "123");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.True(mockHeaders.ContainsKey("Access-Control-Allow-Origin"));
            Assert.Equal("*", mockHeaders["Access-Control-Allow-Origin"].ToString());
            Assert.True(mockHeaders.ContainsKey("Access-Control-Allow-Methods"));
            Assert.True(mockHeaders.ContainsKey("Access-Control-Allow-Headers"));
        }

        [Fact]
        public async Task Run_HandlesConcurrencyIssues_ReturnsConflict()
        {
            // Arrange
            await SeedDatabase();
            
            // Create a mock context that will throw DbUpdateException
            var mockContext = new Mock<ProjectContext>(_dbContextOptions);
            var mockAppointments = new Mock<DbSet<Appointment>>();
            
            // Setup to throw DbUpdateException when SaveChangesAsync is called
            mockContext.Setup(c => c.SaveChangesAsync(default))
                .ThrowsAsync(new DbUpdateException("Concurrency issue"));
            
            var function = new CancelAppointment(_loggerMock.Object, mockContext.Object);
            var mockRequest = new Mock<HttpRequest>();
            var mockHttpContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaders = new HeaderDictionary();

            mockResponse.Setup(r => r.Headers).Returns(mockHeaders);
            mockHttpContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockHttpContext.Object);
            mockRequest.Setup(r => r.Method).Returns("DELETE");

            // Mock the appointment finding logic
            var testAppointment = new Appointment
            {
                Id = 123,
                ClientId = 1,
                ServiceId = 101,
                Time = DateTimeOffset.UtcNow.AddDays(7)
            };

            mockContext.Setup(c => c.Appointments.Include(It.IsAny<string>()).Include(It.IsAny<string>()).FirstOrDefaultAsync(It.IsAny<System.Linq.Expressions.Expression<Func<Appointment, bool>>>(), default))
                .ReturnsAsync(testAppointment);

            // Act
            var result = await function.Run(mockRequest.Object, "123");

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(409, objectResult.StatusCode); // Conflict status code
        }
    }
}
