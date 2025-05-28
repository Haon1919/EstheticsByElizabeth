using System;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
using API.Entities;
using API.Functions;
using API.Tests.Helpers;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace API.Tests.Functions
{
    public class ScheduleAppointmentEdgeCasesTests
    {
        private readonly DbContextOptions<ProjectContext> _dbContextOptions;
        private readonly Mock<ILogger<ScheduleAppointment>> _loggerMock;

        public ScheduleAppointmentEdgeCasesTests()
        {
            // Set up an in-memory database for testing
            _dbContextOptions = new DbContextOptionsBuilder<ProjectContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
            
            // Set up logger mock
            _loggerMock = new Mock<ILogger<ScheduleAppointment>>();
        }

        [Fact]
        public async Task Run_EmptyRequestBody_ReturnsBadRequest()
        {
            // Arrange
            await SeedDatabase();
            
            // Create empty request body
            string emptyJson = "";
            var request = TestHelpers.CreateMockHttpRequest(emptyJson);
            
            var function = new ScheduleAppointment(_loggerMock.Object, CreateContext());

            // Act
            var result = await function.Run(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task Run_MalformedJSON_ReturnsBadRequest()
        {
            // Arrange
            await SeedDatabase();
            
            // Create malformed JSON
            string malformedJson = "{ this is not valid json }";
            var request = TestHelpers.CreateMockHttpRequest(malformedJson);
            
            var function = new ScheduleAppointment(_loggerMock.Object, CreateContext());

            // Act
            var result = await function.Run(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        #region Helper Methods
        
        private ProjectContext CreateContext()
        {
            return new ProjectContext(_dbContextOptions);
        }

        private async Task SeedDatabase()
        {
            using var context = CreateContext();
            
            // Clear any existing data
            context.Clients.RemoveRange(context.Clients);
            context.Services.RemoveRange(context.Services);
            context.Appointments.RemoveRange(context.Appointments);
            context.ClientReviewFlags.RemoveRange(context.ClientReviewFlags);
            await context.SaveChangesAsync();
            
            // Add test data
            context.Clients.Add(new Client
            {
                FirstName = "Test",
                LastName = "User",
                Email = "test@example.com",
                PhoneNumber = "555-555-5555"
            });
            
            context.Services.Add(new Service
            {
                Id = 1,
                Name = "Test Service",
                Duration = 60,
                Description = "A test service",
                Price = 100.00m
            });
            
            await context.SaveChangesAsync();
        }
        
        #endregion
    }
}
