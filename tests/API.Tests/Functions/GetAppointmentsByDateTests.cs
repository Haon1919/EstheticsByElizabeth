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
    public class GetAppointmentsByDateTests
    {
        private readonly DbContextOptions<ProjectContext> _dbContextOptions;
        private readonly Mock<ILogger<GetAppointmentsByDate>> _loggerMock;

        public GetAppointmentsByDateTests()
        {
            // Set up an in-memory database for testing
            _dbContextOptions = new DbContextOptionsBuilder<ProjectContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
            
            // Set up logger mock
            _loggerMock = new Mock<ILogger<GetAppointmentsByDate>>();
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

            // Add appointments for specific dates
            var targetDate = new DateTimeOffset(2025, 6, 15, 10, 0, 0, TimeSpan.Zero);
            var otherDate = new DateTimeOffset(2025, 6, 16, 10, 0, 0, TimeSpan.Zero);

            var appointments = new[]
            {
                new Appointment
                {
                    Id = 1,
                    ClientId = 1,
                    ServiceId = 101,
                    Time = targetDate
                },
                new Appointment
                {
                    Id = 2,
                    ClientId = 2,
                    ServiceId = 101,
                    Time = targetDate.AddHours(2)
                },
                new Appointment
                {
                    Id = 3,
                    ClientId = 1,
                    ServiceId = 101,
                    Time = otherDate // Different date
                }
            };

            context.Appointments.AddRange(appointments);
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task Run_ReturnsAppointmentsForSpecificDate_WhenValidDateProvided()
        {
            // Arrange
            await SeedDatabase();
            var function = new GetAppointmentsByDate(_loggerMock.Object, CreateContext());
            var mockRequest = new Mock<HttpRequest>();
            var mockContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaders = new HeaderDictionary();

            mockResponse.Setup(r => r.Headers).Returns(mockHeaders);
            mockContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockContext.Object);
            mockRequest.Setup(r => r.Method).Returns("GET");

            // Act
            var result = await function.Run(mockRequest.Object, "2025-06-15");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var appointments = Assert.IsAssignableFrom<System.Collections.IEnumerable>(okResult.Value);
            
            var appointmentList = appointments.Cast<object>().ToList();
            Assert.Equal(2, appointmentList.Count); // Should return 2 appointments for 2025-06-15
        }

        [Fact]
        public async Task Run_ReturnsEmptyList_WhenNoAppointmentsExistForDate()
        {
            // Arrange
            await SeedDatabase();
            var function = new GetAppointmentsByDate(_loggerMock.Object, CreateContext());
            var mockRequest = new Mock<HttpRequest>();
            var mockContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaders = new HeaderDictionary();

            mockResponse.Setup(r => r.Headers).Returns(mockHeaders);
            mockContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockContext.Object);
            mockRequest.Setup(r => r.Method).Returns("GET");

            // Act
            var result = await function.Run(mockRequest.Object, "2025-12-25");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var appointments = Assert.IsAssignableFrom<System.Collections.IEnumerable>(okResult.Value);
            
            var appointmentList = appointments.Cast<object>().ToList();
            Assert.Empty(appointmentList);
        }

        [Fact]
        public async Task Run_ReturnsBadRequest_WhenDateParameterIsNull()
        {
            // Arrange
            var function = new GetAppointmentsByDate(_loggerMock.Object, CreateContext());
            var mockRequest = new Mock<HttpRequest>();
            var mockContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaders = new HeaderDictionary();

            mockResponse.Setup(r => r.Headers).Returns(mockHeaders);
            mockContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockContext.Object);
            mockRequest.Setup(r => r.Method).Returns("GET");

            // Act
            var result = await function.Run(mockRequest.Object, null);

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("date parameter", badRequestResult.Value.ToString());
        }

        [Fact]
        public async Task Run_ReturnsBadRequest_WhenDateParameterIsEmpty()
        {
            // Arrange
            var function = new GetAppointmentsByDate(_loggerMock.Object, CreateContext());
            var mockRequest = new Mock<HttpRequest>();
            var mockContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaders = new HeaderDictionary();

            mockResponse.Setup(r => r.Headers).Returns(mockHeaders);
            mockContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockContext.Object);
            mockRequest.Setup(r => r.Method).Returns("GET");

            // Act
            var result = await function.Run(mockRequest.Object, "");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("date parameter", badRequestResult.Value.ToString());
        }

        [Fact]
        public async Task Run_ReturnsBadRequest_WhenDateFormatIsInvalid()
        {
            // Arrange
            var function = new GetAppointmentsByDate(_loggerMock.Object, CreateContext());
            var mockRequest = new Mock<HttpRequest>();
            var mockContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaders = new HeaderDictionary();

            mockResponse.Setup(r => r.Headers).Returns(mockHeaders);
            mockContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockContext.Object);
            mockRequest.Setup(r => r.Method).Returns("GET");

            // Act
            var result = await function.Run(mockRequest.Object, "invalid-date");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Invalid date format", badRequestResult.Value.ToString());
        }

        [Fact]
        public async Task Run_HandlesCorsPreflight_ReturnsOkResult()
        {
            // Arrange
            var function = new GetAppointmentsByDate(_loggerMock.Object, CreateContext());
            var mockRequest = new Mock<HttpRequest>();
            var mockContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaders = new HeaderDictionary();

            mockResponse.Setup(r => r.Headers).Returns(mockHeaders);
            mockContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockContext.Object);
            mockRequest.Setup(r => r.Method).Returns("OPTIONS");

            // Act
            var result = await function.Run(mockRequest.Object, "2025-06-15");

            // Assert
            Assert.IsType<OkResult>(result);
            Assert.True(mockHeaders.ContainsKey("Access-Control-Allow-Origin"));
            Assert.True(mockHeaders.ContainsKey("Access-Control-Allow-Methods"));
        }

        [Fact]
        public async Task Run_ReturnsAppointmentsOrderedByTime()
        {
            // Arrange
            await SeedDatabase();
            var function = new GetAppointmentsByDate(_loggerMock.Object, CreateContext());
            var mockRequest = new Mock<HttpRequest>();
            var mockContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaders = new HeaderDictionary();

            mockResponse.Setup(r => r.Headers).Returns(mockHeaders);
            mockContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockContext.Object);
            mockRequest.Setup(r => r.Method).Returns("GET");

            // Act
            var result = await function.Run(mockRequest.Object, "2025-06-15");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var appointments = okResult.Value as dynamic[];
            
            Assert.NotNull(appointments);
            Assert.Equal(2, appointments.Length);
            
            // Verify appointments are ordered by time (first one should be at 10:00, second at 12:00)
            dynamic firstAppointment = appointments[0];
            dynamic secondAppointment = appointments[1];
            
            // The first appointment should have an earlier time than the second
            var firstTime = DateTimeOffset.Parse(firstAppointment.Time.ToString());
            var secondTime = DateTimeOffset.Parse(secondAppointment.Time.ToString());
            Assert.True(firstTime < secondTime);
        }

        [Fact]
        public async Task Run_IncludesClientAndServiceInformation()
        {
            // Arrange
            await SeedDatabase();
            var function = new GetAppointmentsByDate(_loggerMock.Object, CreateContext());
            var mockRequest = new Mock<HttpRequest>();
            var mockContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaders = new HeaderDictionary();

            mockResponse.Setup(r => r.Headers).Returns(mockHeaders);
            mockContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockContext.Object);
            mockRequest.Setup(r => r.Method).Returns("GET");

            // Act
            var result = await function.Run(mockRequest.Object, "2025-06-15");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var appointments = okResult.Value as dynamic[];
            
            Assert.NotNull(appointments);
            Assert.True(appointments.Length > 0);
            
            dynamic firstAppointment = appointments[0];
            
            // Verify client information is included
            Assert.NotNull(firstAppointment.Client);
            Assert.NotNull(firstAppointment.Client.FirstName);
            Assert.NotNull(firstAppointment.Client.LastName);
            Assert.NotNull(firstAppointment.Client.Email);
            
            // Verify service information is included
            Assert.NotNull(firstAppointment.Service);
            Assert.NotNull(firstAppointment.Service.Name);
            Assert.NotNull(firstAppointment.Service.Price);
            
            // Verify category information is included
            Assert.NotNull(firstAppointment.Service.Category);
            Assert.NotNull(firstAppointment.Service.Category.Name);
        }
    }
}
