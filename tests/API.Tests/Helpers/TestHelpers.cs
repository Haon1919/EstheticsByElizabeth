using System.IO;
using System.Text;
using Microsoft.AspNetCore.Http;
using Moq;
using System.Text.Json;

namespace API.Tests.Helpers
{
    public static class TestHelpers
    {
        /// <summary>
        /// Creates a mock HttpRequest with the given body content
        /// </summary>
        public static HttpRequest CreateMockHttpRequest(object content)
        {
            var json = JsonSerializer.Serialize(content, new JsonSerializerOptions
            {
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase
            });
            
            return CreateMockHttpRequest(json);
        }

        /// <summary>
        /// Creates a mock HttpRequest with the given JSON string
        /// </summary>
        public static HttpRequest CreateMockHttpRequest(string jsonContent)
        {
            var memoryStream = new MemoryStream(Encoding.UTF8.GetBytes(jsonContent));
            memoryStream.Position = 0;
            
            var mockRequest = new Mock<HttpRequest>();
            mockRequest.Setup(x => x.Body).Returns(memoryStream);
            
            return mockRequest.Object;
        }
    }
}
