using Microsoft.AspNetCore.Mvc;
using System.Net.Http.Headers;

namespace TreeDrive.ApiGateway.Controllers;

[ApiController]
[Route("api")]
public class GatewayController : ControllerBase
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ILogger<GatewayController> _logger;

    public GatewayController(IHttpClientFactory httpClientFactory, ILogger<GatewayController> logger)
    {
        _httpClientFactory = httpClientFactory;
        _logger = logger;
    }

    [HttpPost("auth/login")]
    public async Task<IActionResult> Login([FromBody] object request)
    {
        return await ProxyRequest("AuthService", "/api/auth/login", HttpMethod.Post, request);
    }

    [HttpPost("auth/register")]
    public async Task<IActionResult> Register([FromBody] object request)
    {
        return await ProxyRequest("AuthService", "/api/auth/register", HttpMethod.Post, request);
    }

    [HttpGet("files/list")]
    public async Task<IActionResult> ListFiles()
    {
        return await ProxyRequest("FileService", "/api/files/list", HttpMethod.Get);
    }

    [HttpPost("files/upload")]
    public async Task<IActionResult> UploadFile(IFormFile file)
    {
        if (file == null || file.Length == 0)
            return BadRequest(new { error = "No file provided" });

        var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
        var client = _httpClientFactory.CreateClient("FileService");
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        using var formData = new MultipartFormDataContent();
        using var stream = file.OpenReadStream();
        var fileContent = new StreamContent(stream);
        fileContent.Headers.ContentType = new MediaTypeHeaderValue(file.ContentType);
        formData.Add(fileContent, "file", file.FileName);

        try
        {
            var response = await client.PostAsync("/api/files/upload", formData);
            var content = await response.Content.ReadAsStringAsync();
            return Content(content, "application/json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File service error");
            return StatusCode(503, new { error = "File service unavailable" });
        }
    }

    // ✅ NEW: Dedicated download endpoint for binary files
    [HttpGet("files/download/{id}")]
    public async Task<IActionResult> DownloadFile(string id)
    {
        try
        {
            var token = Request.Headers["Authorization"].ToString().Replace("Bearer ", "");
            var client = _httpClientFactory.CreateClient("FileService");
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await client.GetAsync($"/api/files/download/{id}");

            if (!response.IsSuccessStatusCode)
            {
                var errorContent = await response.Content.ReadAsStringAsync();
                return StatusCode((int)response.StatusCode, errorContent);
            }

            // ✅ Read as byte array for binary file download
            var content = await response.Content.ReadAsByteArrayAsync();
            var contentType = response.Content.Headers.ContentType?.MediaType ?? "application/octet-stream";
            
            // Try to get filename from Content-Disposition header
            var contentDisposition = response.Content.Headers.ContentDisposition;
            var filename = contentDisposition?.FileName?.Trim('"') ?? $"file_{id}";

            return File(content, contentType, filename);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "File download error");
            return StatusCode(503, new { error = "File service unavailable" });
        }
    }

    // ✅ NEW: Share file endpoint
    [HttpPost("files/share/{id}")]
    public async Task<IActionResult> ShareFile(string id, [FromBody] object request)
    {
        return await ProxyRequest("FileService", $"/api/files/share/{id}", HttpMethod.Post, request);
    }

    // ✅ NEW: Search users endpoint
    [HttpGet("files/search/users")]
    public async Task<IActionResult> SearchUsers([FromQuery] string query)
    {
        return await ProxyRequest("FileService", $"/api/files/search/users?query={query}", HttpMethod.Get);
    }

    [HttpDelete("files/{id}")]
    public async Task<IActionResult> DeleteFile(string id)
    {
        return await ProxyRequest("FileService", $"/api/files/{id}", HttpMethod.Delete);
    }

    private async Task<IActionResult> ProxyRequest(string serviceName, string path, HttpMethod method, object? body = null)
    {
        try
        {
            var client = _httpClientFactory.CreateClient(serviceName);
            
            var authHeader = Request.Headers["Authorization"].ToString();
            if (!string.IsNullOrEmpty(authHeader))
            {
                client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", 
                    authHeader.Replace("Bearer ", ""));
            }

            HttpResponseMessage response;
            
            if (method == HttpMethod.Get)
            {
                response = await client.GetAsync(path);
            }
            else if (method == HttpMethod.Post && body != null)
            {
                response = await client.PostAsJsonAsync(path, body);
            }
            else if (method == HttpMethod.Delete)
            {
                response = await client.DeleteAsync(path);
            }
            else
            {
                return StatusCode(500, new { error = "Method not supported" });
            }

            var content = await response.Content.ReadAsStringAsync();
            return Content(content, "application/json");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error proxying request to {ServiceName}", serviceName);
            return StatusCode(503, new { error = $"{serviceName} unavailable" });
        }
    }
}
