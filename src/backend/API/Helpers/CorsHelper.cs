using Microsoft.Azure.Functions.Worker.Http;
using System.Net;

namespace API.Helpers
{
    public static class CorsHelper
    {
        public static void AddCorsHeaders(HttpResponseData response, string origin = "*")
        {
            response.Headers.Add("Access-Control-Allow-Origin", origin);
            response.Headers.Add("Access-Control-Allow-Methods", "GET, POST, PUT, DELETE, OPTIONS");
            response.Headers.Add("Access-Control-Allow-Headers", "Content-Type, Authorization, Origin, Accept, X-Requested-With");
            response.Headers.Add("Access-Control-Max-Age", "86400");
        }

        public static HttpResponseData CreateCorsResponse(HttpRequestData req, HttpStatusCode statusCode = HttpStatusCode.OK)
        {
            var response = req.CreateResponse(statusCode);
            AddCorsHeaders(response);
            return response;
        }
    }
}
