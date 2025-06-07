using System;
using System.IO;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using API.Data;
using API.DTOs;
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
    public class SubmitContactFormTests
    {
        private readonly DbContextOptions<ProjectContext> _dbContextOptions;
        private readonly Mock<ILogger<SubmitContactForm>> _loggerMock;

        public SubmitContactFormTests()
        {
            // Set up an in-memory database for testing
            _dbContextOptions = new DbContextOptionsBuilder<ProjectContext>()
                .UseInMemoryDatabase(databaseName: $"TestDb_{Guid.NewGuid()}")
                .Options;
            
            // Set up logger mock
            _loggerMock = new Mock<ILogger<SubmitContactForm>>();
        }

        private ProjectContext CreateContext()
        {
            return new ProjectContext(_dbContextOptions);
        }

        private HttpRequest CreateMockRequest(string requestBody, string method = "POST")
        {
            var context = new DefaultHttpContext();
            var request = context.Request;
            
            request.Method = method;
            request.ContentType = "application/json";
            
            if (!string.IsNullOrEmpty(requestBody))
            {
                var bodyBytes = Encoding.UTF8.GetBytes(requestBody);
                request.Body = new MemoryStream(bodyBytes);
            }

            return request;
        }

        [Fact]
        public async Task SubmitContactForm_ValidContactData_ReturnsOkResult()
        {
            // Arrange
            using var context = CreateContext();
            var function = new SubmitContactForm(_loggerMock.Object, context);

            var contactDto = new ContactSubmissionDto
            {
                Name = "Jane Smith",
                Email = "jane.smith@example.com",
                Phone = "555-123-4567",
                Subject = "Service Inquiry",
                Message = "I would like to know more about your facial treatments."
            };

            var requestBody = JsonSerializer.Serialize(contactDto);
            var request = CreateMockRequest(requestBody);

            // Act
            var result = await function.Run(request);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            var okResult = result as OkObjectResult;
            Assert.NotNull(okResult);

            // Verify the response contains success information
            var responseValue = okResult.Value;
            Assert.NotNull(responseValue);
            
            // Check that contact submission was saved to database
            var savedSubmission = await context.ContactSubmissions.FirstOrDefaultAsync();
            Assert.NotNull(savedSubmission);
            Assert.Equal(contactDto.Name, savedSubmission.Name);
            Assert.Equal(contactDto.Email.ToLowerInvariant(), savedSubmission.Email);
            Assert.Equal(contactDto.Phone, savedSubmission.Phone);
            Assert.Equal(contactDto.Subject, savedSubmission.Subject);
            Assert.Equal(contactDto.Message, savedSubmission.Message);
            Assert.False(savedSubmission.IsRead);
            Assert.True(savedSubmission.SubmittedAt > DateTimeOffset.MinValue);
        }

        [Fact]
        public async Task SubmitContactForm_EmptyRequestBody_ReturnsBadRequest()
        {
            // Arrange
            using var context = CreateContext();
            var function = new SubmitContactForm(_loggerMock.Object, context);
            var request = CreateMockRequest("");

            // Act
            var result = await function.Run(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
            var badResult = result as BadRequestObjectResult;
            Assert.NotNull(badResult);
        }

        [Fact]
        public async Task SubmitContactForm_InvalidJson_ReturnsBadRequest()
        {
            // Arrange
            using var context = CreateContext();
            var function = new SubmitContactForm(_loggerMock.Object, context);
            var request = CreateMockRequest("{ invalid json }");

            // Act
            var result = await function.Run(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task SubmitContactForm_MissingRequiredFields_ReturnsBadRequest()
        {
            // Arrange
            using var context = CreateContext();
            var function = new SubmitContactForm(_loggerMock.Object, context);

            var incompleteContactDto = new ContactSubmissionDto
            {
                Name = "John Doe",
                Email = "", // Missing email
                Subject = "Test",
                Message = "Test message"
            };

            var requestBody = JsonSerializer.Serialize(incompleteContactDto);
            var request = CreateMockRequest(requestBody);

            // Act
            var result = await function.Run(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task SubmitContactForm_InvalidEmail_ReturnsBadRequest()
        {
            // Arrange
            using var context = CreateContext();
            var function = new SubmitContactForm(_loggerMock.Object, context);

            var contactDto = new ContactSubmissionDto
            {
                Name = "John Doe",
                Email = "invalid-email", // Invalid email format
                Phone = "555-123-4567",
                Subject = "Test",
                Message = "Test message"
            };

            var requestBody = JsonSerializer.Serialize(contactDto);
            var request = CreateMockRequest(requestBody);

            // Act
            var result = await function.Run(request);

            // Assert
            Assert.IsType<BadRequestObjectResult>(result);
        }

        [Fact]
        public async Task SubmitContactForm_OptionsRequest_ReturnsOkWithCorsHeaders()
        {
            // Arrange
            using var context = CreateContext();
            var function = new SubmitContactForm(_loggerMock.Object, context);
            var request = CreateMockRequest("", "OPTIONS");

            // Act
            var result = await function.Run(request);

            // Assert
            Assert.IsType<OkResult>(result);
            
            // Verify CORS headers are added (would need to check HttpContext.Response.Headers in real scenario)
            Assert.True(request.HttpContext.Response.Headers.ContainsKey("Access-Control-Allow-Origin"));
        }

        [Fact]
        public async Task SubmitContactForm_ValidDataWithOptionalPhone_ReturnsOkResult()
        {
            // Arrange
            using var context = CreateContext();
            var function = new SubmitContactForm(_loggerMock.Object, context);

            var contactDto = new ContactSubmissionDto
            {
                Name = "Alice Johnson",
                Email = "alice@example.com",
                Phone = null, // Optional phone field
                Subject = "Booking Information",
                Message = "I would like to schedule an appointment for next week."
            };

            var requestBody = JsonSerializer.Serialize(contactDto);
            var request = CreateMockRequest(requestBody);

            // Act
            var result = await function.Run(request);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            
            // Check that contact submission was saved with null phone
            var savedSubmission = await context.ContactSubmissions.FirstOrDefaultAsync();
            Assert.NotNull(savedSubmission);
            Assert.Equal(contactDto.Name, savedSubmission.Name);
            Assert.Equal(contactDto.Email.ToLowerInvariant(), savedSubmission.Email);
            Assert.Null(savedSubmission.Phone);
            Assert.Equal(contactDto.Subject, savedSubmission.Subject);
            Assert.Equal(contactDto.Message, savedSubmission.Message);
        }

        [Fact]
        public async Task SubmitContactForm_TrimWhitespaceFromFields_SavesCleanData()
        {
            // Arrange
            using var context = CreateContext();
            var function = new SubmitContactForm(_loggerMock.Object, context);

            var contactDto = new ContactSubmissionDto
            {
                Name = "  Bob Williams  ", // Extra whitespace
                Email = "  BOB@EXAMPLE.COM  ", // Extra whitespace and uppercase
                Phone = "  555-987-6543  ",
                Subject = "  Pricing Information  ",
                Message = "  How much do your services cost?  "
            };

            var requestBody = JsonSerializer.Serialize(contactDto);
            var request = CreateMockRequest(requestBody);

            // Act
            var result = await function.Run(request);

            // Assert
            Assert.IsType<OkObjectResult>(result);
            
            // Check that data was trimmed and email lowercased
            var savedSubmission = await context.ContactSubmissions.FirstOrDefaultAsync();
            Assert.NotNull(savedSubmission);
            Assert.Equal("Bob Williams", savedSubmission.Name);
            Assert.Equal("bob@example.com", savedSubmission.Email);
            Assert.Equal("555-987-6543", savedSubmission.Phone);
            Assert.Equal("Pricing Information", savedSubmission.Subject);
            Assert.Equal("How much do your services cost?", savedSubmission.Message);
        }
    }
}
