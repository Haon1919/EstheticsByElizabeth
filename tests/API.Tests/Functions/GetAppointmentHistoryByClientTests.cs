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
    public class GetAppointmentHistoryByClientTests
    {
        private readonly DbContextOptions<ProjectContext> _dbContextOptions;
        private readonly Mock<ILogger<GetAppointmentHistoryByClient>> _loggerMock;

        public GetAppointmentHistoryByClientTests()
        {
            // Set up an in-memory database for testing
            _dbContextOptions = new DbContextOptionsBuilder<ProjectContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
            
            // Set up logger mock
            _loggerMock = new Mock<ILogger<GetAppointmentHistoryByClient>>();
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

            // Add clients
            var client1 = new Client
            {
                Id = 1,
                FirstName = "John",
                LastName = "Doe",
                Email = "john.doe@example.com",
                PhoneNumber = "555-123-4567"
            };
            
            var client2 = new Client
            {
                Id = 2,
                FirstName = "Jane",
                LastName = "Smith",
                Email = "jane.smith@example.com",
                PhoneNumber = "555-987-6543"
            };
            
            context.Clients.AddRange(client1, client2);
            await context.SaveChangesAsync();

            // Add appointments for client1 (john.doe@example.com)
            var appointments = new[]
            {
                new Appointment
                {
                    Id = 1,
                    ClientId = 1,
                    ServiceId = 101,
                    Time = DateTimeOffset.UtcNow.AddDays(-30) // 30 days ago
                },
                new Appointment
                {
                    Id = 2,
                    ClientId = 1,
                    ServiceId = 101,
                    Time = DateTimeOffset.UtcNow.AddDays(-15) // 15 days ago
                },
                new Appointment
                {
                    Id = 3,
                    ClientId = 2, // Different client
                    ServiceId = 101,
                    Time = DateTimeOffset.UtcNow.AddDays(-10)
                }
            };

            context.Appointments.AddRange(appointments);
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task Run_ReturnsClientAppointmentHistory_WhenValidEmailProvided()
        {
            // Arrange
            await SeedDatabase();
            var function = new GetAppointmentHistoryByClient(_loggerMock.Object, CreateContext());
            var mockRequest = new Mock<HttpRequest>();
            var mockContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaders = new HeaderDictionary();
            var mockQuery = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "email", "john.doe@example.com" }
            });

            mockResponse.Setup(r => r.Headers).Returns(mockHeaders);
            mockContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockContext.Object);
            mockRequest.Setup(r => r.Method).Returns("GET");
            mockRequest.Setup(r => r.Query).Returns(mockQuery);

            // Act
            var result = await function.Run(mockRequest.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value as dynamic;
            
            Assert.NotNull(response);
            Assert.NotNull(response.Client);
            Assert.NotNull(response.Appointments);
            
            // Check client information
            Assert.Equal("John", response.Client.FirstName.ToString());
            Assert.Equal("Doe", response.Client.LastName.ToString());
            Assert.Equal("john.doe@example.com", response.Client.Email.ToString());
            
            // Check appointments count
            var appointments = response.Appointments as dynamic[];
            Assert.Equal(2, appointments.Length); // Should return 2 appointments for this client
            Assert.Equal(2, (int)response.TotalCount);
        }

        [Fact]
        public async Task Run_ReturnsNotFound_WhenClientEmailDoesNotExist()
        {
            // Arrange
            await SeedDatabase();
            var function = new GetAppointmentHistoryByClient(_loggerMock.Object, CreateContext());
            var mockRequest = new Mock<HttpRequest>();
            var mockContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaders = new HeaderDictionary();
            var mockQuery = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "email", "nonexistent@example.com" }
            });

            mockResponse.Setup(r => r.Headers).Returns(mockHeaders);
            mockContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockContext.Object);
            mockRequest.Setup(r => r.Method).Returns("GET");
            mockRequest.Setup(r => r.Query).Returns(mockQuery);

            // Act
            var result = await function.Run(mockRequest.Object);

            // Assert
            var notFoundResult = Assert.IsType<NotFoundObjectResult>(result);
            Assert.Contains("No client found with email", notFoundResult.Value.ToString());
        }

        [Fact]
        public async Task Run_ReturnsBadRequest_WhenEmailParameterIsEmpty()
        {
            // Arrange
            var function = new GetAppointmentHistoryByClient(_loggerMock.Object, CreateContext());
            var mockRequest = new Mock<HttpRequest>();
            var mockContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaders = new HeaderDictionary();
            var mockQuery = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>());

            mockResponse.Setup(r => r.Headers).Returns(mockHeaders);
            mockContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockContext.Object);
            mockRequest.Setup(r => r.Method).Returns("GET");
            mockRequest.Setup(r => r.Query).Returns(mockQuery);

            // Act
            var result = await function.Run(mockRequest.Object);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("email parameter", badRequestResult.Value.ToString());
        }

        [Fact]
        public async Task Run_HandlesCorsPreflight_ReturnsOkResult()
        {
            // Arrange
            var function = new GetAppointmentHistoryByClient(_loggerMock.Object, CreateContext());
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
        public async Task Run_ReturnsAppointmentsOrderedByTimeDescending()
        {
            // Arrange
            await SeedDatabase();
            var function = new GetAppointmentHistoryByClient(_loggerMock.Object, CreateContext());
            var mockRequest = new Mock<HttpRequest>();
            var mockContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaders = new HeaderDictionary();
            var mockQuery = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "email", "john.doe@example.com" }
            });

            mockResponse.Setup(r => r.Headers).Returns(mockHeaders);
            mockContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockContext.Object);
            mockRequest.Setup(r => r.Method).Returns("GET");
            mockRequest.Setup(r => r.Query).Returns(mockQuery);

            // Act
            var result = await function.Run(mockRequest.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value as dynamic;
            var appointments = response.Appointments as dynamic[];
            
            Assert.NotNull(appointments);
            Assert.Equal(2, appointments.Length);
            
            // Verify appointments are ordered by time descending (most recent first)
            dynamic firstAppointment = appointments[0];
            dynamic secondAppointment = appointments[1];
            
            var firstTime = DateTimeOffset.Parse(firstAppointment.Time.ToString());
            var secondTime = DateTimeOffset.Parse(secondAppointment.Time.ToString());
            Assert.True(firstTime > secondTime); // First should be more recent
        }

        [Fact]
        public async Task Run_IncludesServiceAndCategoryInformation()
        {
            // Arrange
            await SeedDatabase();
            var function = new GetAppointmentHistoryByClient(_loggerMock.Object, CreateContext());
            var mockRequest = new Mock<HttpRequest>();
            var mockContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaders = new HeaderDictionary();
            var mockQuery = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "email", "john.doe@example.com" }
            });

            mockResponse.Setup(r => r.Headers).Returns(mockHeaders);
            mockContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockContext.Object);
            mockRequest.Setup(r => r.Method).Returns("GET");
            mockRequest.Setup(r => r.Query).Returns(mockQuery);

            // Act
            var result = await function.Run(mockRequest.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value as dynamic;
            var appointments = response.Appointments as dynamic[];
            
            Assert.NotNull(appointments);
            Assert.True(appointments.Length > 0);
            
            dynamic firstAppointment = appointments[0];
            
            // Verify service information is included
            Assert.NotNull(firstAppointment.Service);
            Assert.NotNull(firstAppointment.Service.Name);
            Assert.NotNull(firstAppointment.Service.Description);
            Assert.NotNull(firstAppointment.Service.Price);
            Assert.NotNull(firstAppointment.Service.Duration);
            
            // Verify category information is included
            Assert.NotNull(firstAppointment.Service.Category);
            Assert.NotNull(firstAppointment.Service.Category.Name);
        }

        [Fact]
        public async Task Run_ReturnsEmptyAppointmentsList_WhenClientHasNoAppointments()
        {
            // Arrange
            using var context = CreateContext();
            
            // Add only a client without appointments
            var client = new Client
            {
                Id = 1,
                FirstName = "Empty",
                LastName = "Client",
                Email = "empty@example.com",
                PhoneNumber = "555-000-0000"
            };
            
            context.Clients.Add(client);
            await context.SaveChangesAsync();

            var function = new GetAppointmentHistoryByClient(_loggerMock.Object, CreateContext());
            var mockRequest = new Mock<HttpRequest>();
            var mockHttpContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaders = new HeaderDictionary();
            var mockQuery = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "email", "empty@example.com" }
            });

            mockResponse.Setup(r => r.Headers).Returns(mockHeaders);
            mockHttpContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockHttpContext.Object);
            mockRequest.Setup(r => r.Method).Returns("GET");
            mockRequest.Setup(r => r.Query).Returns(mockQuery);

            // Act
            var result = await function.Run(mockRequest.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value as dynamic;
            
            Assert.NotNull(response);
            Assert.Equal("Empty", response.Client.FirstName.ToString());
            
            var appointments = response.Appointments as dynamic[];
            Assert.Empty(appointments);
            Assert.Equal(0, (int)response.TotalCount);
        }

        [Fact]
        public async Task Run_IncludesClientInformationInEachAppointment()
        {
            // Arrange
            await SeedDatabase();
            var function = new GetAppointmentHistoryByClient(_loggerMock.Object, CreateContext());
            var mockRequest = new Mock<HttpRequest>();
            var mockContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaders = new HeaderDictionary();
            var mockQuery = new QueryCollection(new Dictionary<string, Microsoft.Extensions.Primitives.StringValues>
            {
                { "email", "john.doe@example.com" }
            });

            mockResponse.Setup(r => r.Headers).Returns(mockHeaders);
            mockContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockContext.Object);
            mockRequest.Setup(r => r.Method).Returns("GET");
            mockRequest.Setup(r => r.Query).Returns(mockQuery);

            // Act
            var result = await function.Run(mockRequest.Object);

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value as dynamic;
            var appointments = response.Appointments as dynamic[];
            
            Assert.NotNull(appointments);
            Assert.True(appointments.Length > 0);
            
            dynamic firstAppointment = appointments[0];
            
            // Verify client information is included in each appointment
            Assert.NotNull(firstAppointment.Client);
            Assert.NotNull(firstAppointment.Client.Id);
            Assert.NotNull(firstAppointment.Client.FirstName);
            Assert.NotNull(firstAppointment.Client.LastName);
            Assert.NotNull(firstAppointment.Client.Email);
            Assert.NotNull(firstAppointment.Client.PhoneNumber);
            
            // Verify the client data matches the expected values
            Assert.Equal("John", firstAppointment.Client.FirstName.ToString());
            Assert.Equal("Doe", firstAppointment.Client.LastName.ToString());
            Assert.Equal("john.doe@example.com", firstAppointment.Client.Email.ToString());
            Assert.Equal("555-123-4567", firstAppointment.Client.PhoneNumber.ToString());
        }
    }
}
