using System;
using System.Linq;
using System.Threading.Tasks;
using API.Data;
using API.Entities;
using API.Functions;
using API.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace API.Tests.Functions
{
    public class UnbanClientTests
    {
        private readonly DbContextOptions<ProjectContext> _dbContextOptions;
        private readonly Mock<ILogger<UnbanClient>> _loggerMock;
        private readonly Mock<ClientReviewService> _reviewServiceMock;

        public UnbanClientTests()
        {
            // Set up an in-memory database for testing
            _dbContextOptions = new DbContextOptionsBuilder<ProjectContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
            
            // Set up logger mock
            _loggerMock = new Mock<ILogger<UnbanClient>>();
            
            // Set up review service mock
            var reviewServiceLoggerMock = new Mock<ILogger<ClientReviewService>>();
            _reviewServiceMock = new Mock<ClientReviewService>(reviewServiceLoggerMock.Object, CreateContext());
        }

        private ProjectContext CreateContext()
        {
            return new ProjectContext(_dbContextOptions);
        }

        private async Task SeedDatabase()
        {
            using var context = CreateContext();
            
            // Add clients
            var bannedClient = new Client 
            { 
                Id = 1, 
                FirstName = "Banned", 
                LastName = "Client", 
                Email = "banned@example.com", 
                PhoneNumber = "555-BANNED" 
            };
            
            var normalClient = new Client 
            { 
                Id = 2, 
                FirstName = "Normal", 
                LastName = "Client", 
                Email = "normal@example.com", 
                PhoneNumber = "555-NORMAL" 
            };

            context.Clients.AddRange(bannedClient, normalClient);
            await context.SaveChangesAsync();

            // Add appointments for the clients
            var appointment1 = new Appointment
            {
                Id = 1,
                ClientId = 1,
                ServiceId = 1,
                Time = DateTimeOffset.UtcNow.AddDays(-1)
            };

            var appointment2 = new Appointment
            {
                Id = 2,
                ClientId = 2,
                ServiceId = 1,
                Time = DateTimeOffset.UtcNow.AddDays(-1)
            };

            context.Appointments.AddRange(appointment1, appointment2);
            await context.SaveChangesAsync();

            // Add ban flags for banned client
            var banFlag1 = new ClientReviewFlag
            {
                Id = 1,
                ClientId = 1,
                AppointmentId = 1,
                FlagReason = "Multiple bookings on same day",
                FlagDate = DateTimeOffset.UtcNow.AddDays(-2),
                Status = "BANNED",
                ReviewedBy = "Admin",
                ReviewDate = DateTimeOffset.UtcNow.AddDays(-1),
                AdminComments = "Client banned for abuse"
            };

            var banFlag2 = new ClientReviewFlag
            {
                Id = 2,
                ClientId = 1,
                AppointmentId = 1,
                FlagReason = "Inappropriate behavior",
                FlagDate = DateTimeOffset.UtcNow.AddDays(-1),
                Status = "BANNED",
                ReviewedBy = "Admin",
                ReviewDate = DateTimeOffset.UtcNow.AddHours(-12),
                AdminComments = "Second ban flag"
            };

            context.ClientReviewFlags.AddRange(banFlag1, banFlag2);
            await context.SaveChangesAsync();
        }

        [Fact]
        public async Task Run_ValidClientId_SuccessfullyUnbansClient()
        {
            // Arrange
            await SeedDatabase();
            var reviewService = new ClientReviewService(Mock.Of<ILogger<ClientReviewService>>(), CreateContext());
            var function = new UnbanClient(_loggerMock.Object, CreateContext(), reviewService);
            var mockRequest = new Mock<HttpRequest>();
            var mockContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaders = new HeaderDictionary();

            mockResponse.Setup(r => r.Headers).Returns(mockHeaders);
            mockContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockContext.Object);
            mockRequest.Setup(r => r.Method).Returns("DELETE");

            // Act
            var result = await function.Run(mockRequest.Object, "1");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value as dynamic;
            Assert.NotNull(response);
            
            // Verify the response contains the expected message and count
            var message = response.GetType().GetProperty("Message")?.GetValue(response)?.ToString();
            Assert.Contains("Client with ID 1 has been unbanned", message);
            
            var unbannedFlags = response.GetType().GetProperty("UnbannedFlags")?.GetValue(response);
            Assert.Equal(2, unbannedFlags);

            // Verify ban flags are updated in database
            using var context = CreateContext();
            var updatedFlags = await context.ClientReviewFlags
                .Where(rf => rf.ClientId == 1 && rf.Status == "RESOLVED")
                .ToListAsync();
            Assert.Equal(2, updatedFlags.Count);
        }

        [Fact]
        public async Task Run_InvalidClientIdFormat_ReturnsBadRequest()
        {
            // Arrange
            var reviewService = new ClientReviewService(Mock.Of<ILogger<ClientReviewService>>(), CreateContext());
            var function = new UnbanClient(_loggerMock.Object, CreateContext(), reviewService);
            var mockRequest = new Mock<HttpRequest>();
            var mockContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaders = new HeaderDictionary();

            mockResponse.Setup(r => r.Headers).Returns(mockHeaders);
            mockContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockContext.Object);
            mockRequest.Setup(r => r.Method).Returns("DELETE");

            // Act
            var result = await function.Run(mockRequest.Object, "invalid");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Invalid client ID format", badRequestResult.Value);
        }

        [Fact]
        public async Task Run_ClientNotFound_ReturnsNotFound()
        {
            // Arrange
            await SeedDatabase();
            var reviewService = new ClientReviewService(Mock.Of<ILogger<ClientReviewService>>(), CreateContext());
            var function = new UnbanClient(_loggerMock.Object, CreateContext(), reviewService);
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
            Assert.Equal("Client with ID 999 not found", notFoundResult.Value);
        }

        [Fact]
        public async Task Run_ClientNotBanned_ReturnsBadRequest()
        {
            // Arrange
            await SeedDatabase();
            var reviewService = new ClientReviewService(Mock.Of<ILogger<ClientReviewService>>(), CreateContext());
            var function = new UnbanClient(_loggerMock.Object, CreateContext(), reviewService);
            var mockRequest = new Mock<HttpRequest>();
            var mockContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaders = new HeaderDictionary();

            mockResponse.Setup(r => r.Headers).Returns(mockHeaders);
            mockContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockContext.Object);
            mockRequest.Setup(r => r.Method).Returns("DELETE");

            // Act - Using client ID 2 which has no ban flags
            var result = await function.Run(mockRequest.Object, "2");

            // Assert
            var badRequestResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Equal("Client with ID 2 is not currently banned", badRequestResult.Value);
        }

        [Fact]
        public async Task Run_HandlesCorsPreflight_ReturnsOkResult()
        {
            // Arrange
            var reviewService = new ClientReviewService(Mock.Of<ILogger<ClientReviewService>>(), CreateContext());
            var function = new UnbanClient(_loggerMock.Object, CreateContext(), reviewService);
            var mockRequest = new Mock<HttpRequest>();
            var mockContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaders = new HeaderDictionary();

            mockResponse.Setup(r => r.Headers).Returns(mockHeaders);
            mockContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockContext.Object);
            mockRequest.Setup(r => r.Method).Returns("OPTIONS");

            // Act
            var result = await function.Run(mockRequest.Object, "1");

            // Assert
            Assert.IsType<OkResult>(result);
            Assert.True(mockHeaders.ContainsKey("Access-Control-Allow-Origin"));
            Assert.True(mockHeaders.ContainsKey("Access-Control-Allow-Methods"));
            Assert.True(mockHeaders.ContainsKey("Access-Control-Allow-Headers"));
            Assert.True(mockHeaders.ContainsKey("Access-Control-Max-Age"));
        }

        [Fact]
        public async Task Run_SetsCorrectCorsHeaders_ForDeleteRequest()
        {
            // Arrange
            await SeedDatabase();
            var reviewService = new ClientReviewService(Mock.Of<ILogger<ClientReviewService>>(), CreateContext());
            var function = new UnbanClient(_loggerMock.Object, CreateContext(), reviewService);
            var mockRequest = new Mock<HttpRequest>();
            var mockContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaders = new HeaderDictionary();

            mockResponse.Setup(r => r.Headers).Returns(mockHeaders);
            mockContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockContext.Object);
            mockRequest.Setup(r => r.Method).Returns("DELETE");

            // Act
            var result = await function.Run(mockRequest.Object, "1");

            // Assert
            Assert.IsType<OkObjectResult>(result);
            Assert.True(mockHeaders.ContainsKey("Access-Control-Allow-Origin"));
            Assert.Equal("*", mockHeaders["Access-Control-Allow-Origin"].ToString());
            Assert.True(mockHeaders.ContainsKey("Access-Control-Allow-Methods"));
            Assert.Equal("DELETE, OPTIONS", mockHeaders["Access-Control-Allow-Methods"].ToString());
        }

        [Fact]
        public async Task Run_UpdatesBanFlagsCorrectly_WithProperTimestamps()
        {
            // Arrange
            await SeedDatabase();
            var reviewService = new ClientReviewService(Mock.Of<ILogger<ClientReviewService>>(), CreateContext());
            var function = new UnbanClient(_loggerMock.Object, CreateContext(), reviewService);
            var mockRequest = new Mock<HttpRequest>();
            var mockContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaders = new HeaderDictionary();

            mockResponse.Setup(r => r.Headers).Returns(mockHeaders);
            mockContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockContext.Object);
            mockRequest.Setup(r => r.Method).Returns("DELETE");

            var beforeUnban = DateTimeOffset.UtcNow;

            // Act
            var result = await function.Run(mockRequest.Object, "1");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            
            // Verify flags are updated correctly
            using var context = CreateContext();
            var updatedFlags = await context.ClientReviewFlags
                .Where(rf => rf.ClientId == 1)
                .ToListAsync();

            Assert.All(updatedFlags, flag =>
            {
                Assert.Equal("RESOLVED", flag.Status);
                Assert.Equal("System (Unban Request)", flag.ReviewedBy);
                Assert.Equal("Ban removed via DELETE endpoint", flag.AdminComments);
                Assert.True(flag.ReviewDate >= beforeUnban);
                Assert.True(flag.ReviewDate <= DateTimeOffset.UtcNow);
            });
        }

        [Fact]
        public async Task Run_HandlesDbUpdateException_ReturnsConflictResult()
        {
            // Arrange
            var mockContext = new Mock<ProjectContext>(new DbContextOptionsBuilder<ProjectContext>()
                .UseInMemoryDatabase($"ErrorTestDb_{Guid.NewGuid()}")
                .Options);
            
            var mockClients = new Mock<DbSet<Client>>();
            var mockFlags = new Mock<DbSet<ClientReviewFlag>>();
            
            // Setup the client to be found but SaveChangesAsync to throw DbUpdateException
            var client = new Client { Id = 1, Email = "test@example.com", ReviewFlags = new List<ClientReviewFlag>() };
            var queryableClients = new List<Client> { client }.AsQueryable();
            
            mockClients.As<IQueryable<Client>>().Setup(m => m.Provider).Returns(queryableClients.Provider);
            mockClients.As<IQueryable<Client>>().Setup(m => m.Expression).Returns(queryableClients.Expression);
            mockClients.As<IQueryable<Client>>().Setup(m => m.ElementType).Returns(queryableClients.ElementType);
            mockClients.As<IQueryable<Client>>().Setup(m => m.GetEnumerator()).Returns(queryableClients.GetEnumerator());

            mockContext.Setup(c => c.Clients).Returns(mockClients.Object);
            mockContext.Setup(c => c.SaveChangesAsync(default)).ThrowsAsync(new DbUpdateException("Database error"));
            
            var reviewService = new ClientReviewService(Mock.Of<ILogger<ClientReviewService>>(), mockContext.Object);
            var function = new UnbanClient(_loggerMock.Object, mockContext.Object, reviewService);
            var mockRequest = new Mock<HttpRequest>();
            var mockHttpContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaders = new HeaderDictionary();

            mockResponse.Setup(r => r.Headers).Returns(mockHeaders);
            mockHttpContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockHttpContext.Object);
            mockRequest.Setup(r => r.Method).Returns("DELETE");

            // Act
            var result = await function.Run(mockRequest.Object, "1");

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(409, objectResult.StatusCode);
            Assert.Contains("database error occurred", objectResult.Value?.ToString()?.ToLower());
        }

        [Fact]
        public async Task Run_HandlesGenericException_ReturnsInternalServerError()
        {
            // Arrange
            var mockContext = new Mock<ProjectContext>(new DbContextOptionsBuilder<ProjectContext>()
                .UseInMemoryDatabase($"ErrorTestDb_{Guid.NewGuid()}")
                .Options);
            
            // Setup the mock to throw a generic exception
            mockContext.Setup(c => c.Clients).Throws(new Exception("Unexpected error"));
            
            var reviewService = new ClientReviewService(Mock.Of<ILogger<ClientReviewService>>(), mockContext.Object);
            var function = new UnbanClient(_loggerMock.Object, mockContext.Object, reviewService);
            var mockRequest = new Mock<HttpRequest>();
            var mockHttpContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaders = new HeaderDictionary();

            mockResponse.Setup(r => r.Headers).Returns(mockHeaders);
            mockHttpContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockHttpContext.Object);
            mockRequest.Setup(r => r.Method).Returns("DELETE");

            // Act
            var result = await function.Run(mockRequest.Object, "1");

            // Assert
            var objectResult = Assert.IsType<ObjectResult>(result);
            Assert.Equal(500, objectResult.StatusCode);
            Assert.Contains("unexpected error occurred", objectResult.Value?.ToString()?.ToLower());
        }

        [Fact]
        public async Task Run_ClientWithMixedStatusFlags_OnlyUnbansBannedFlags()
        {
            // Arrange
            using var context = CreateContext();
            
            // Add client
            var client = new Client 
            { 
                Id = 3, 
                FirstName = "Mixed", 
                LastName = "Status", 
                Email = "mixed@example.com", 
                PhoneNumber = "555-MIXED" 
            };
            
            context.Clients.Add(client);
            await context.SaveChangesAsync();

            // Add appointment
            var appointment = new Appointment
            {
                Id = 3,
                ClientId = 3,
                ServiceId = 1,
                Time = DateTimeOffset.UtcNow.AddDays(-1)
            };

            context.Appointments.Add(appointment);
            await context.SaveChangesAsync();

            // Add flags with different statuses
            var bannedFlag = new ClientReviewFlag
            {
                ClientId = 3,
                AppointmentId = 3,
                FlagReason = "Banned reason",
                FlagDate = DateTimeOffset.UtcNow.AddDays(-2),
                Status = "BANNED"
            };

            var pendingFlag = new ClientReviewFlag
            {
                ClientId = 3,
                AppointmentId = 3,
                FlagReason = "Pending reason",
                FlagDate = DateTimeOffset.UtcNow.AddDays(-1),
                Status = "PENDING"
            };

            var approvedFlag = new ClientReviewFlag
            {
                ClientId = 3,
                AppointmentId = 3,
                FlagReason = "Approved reason",
                FlagDate = DateTimeOffset.UtcNow.AddDays(-3),
                Status = "APPROVED"
            };

            context.ClientReviewFlags.AddRange(bannedFlag, pendingFlag, approvedFlag);
            await context.SaveChangesAsync();

            var reviewService = new ClientReviewService(Mock.Of<ILogger<ClientReviewService>>(), CreateContext());
            var function = new UnbanClient(_loggerMock.Object, CreateContext(), reviewService);
            var mockRequest = new Mock<HttpRequest>();
            var mockHttpContext = new Mock<HttpContext>();
            var mockResponse = new Mock<HttpResponse>();
            var mockHeaders = new HeaderDictionary();

            mockResponse.Setup(r => r.Headers).Returns(mockHeaders);
            mockHttpContext.Setup(c => c.Response).Returns(mockResponse.Object);
            mockRequest.Setup(r => r.HttpContext).Returns(mockHttpContext.Object);
            mockRequest.Setup(r => r.Method).Returns("DELETE");

            // Act
            var result = await function.Run(mockRequest.Object, "3");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            var response = okResult.Value as dynamic;
            var unbannedFlags = response.GetType().GetProperty("UnbannedFlags")?.GetValue(response);
            Assert.Equal(1, unbannedFlags); // Only the banned flag should be updated

            // Verify only banned flag was updated
            using var verifyContext = CreateContext();
            var allFlags = await verifyContext.ClientReviewFlags
                .Where(rf => rf.ClientId == 3)
                .ToListAsync();

            var resolvedFlags = allFlags.Where(f => f.Status == "RESOLVED").ToList();
            var unchangedFlags = allFlags.Where(f => f.Status != "RESOLVED").ToList();

            Assert.Single(resolvedFlags);
            Assert.Equal("Banned reason", resolvedFlags.First().FlagReason);
            Assert.Equal(2, unchangedFlags.Count);
        }
    }
}
