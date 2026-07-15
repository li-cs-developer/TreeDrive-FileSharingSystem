var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// ✅ FIX: Use service names inside Docker
builder.Services.AddHttpClient("AuthService", client =>
{
    // In Docker, use the service name. For local development, use localhost.
    var baseUrl = builder.Configuration["Services:AuthService"] ?? 
                   (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true" 
                       ? "http://auth-service:80" 
                       : "http://localhost:5001");
    client.BaseAddress = new Uri(baseUrl);
});

builder.Services.AddHttpClient("FileService", client =>
{
    var baseUrl = builder.Configuration["Services:FileService"] ?? 
                   (Environment.GetEnvironmentVariable("DOTNET_RUNNING_IN_CONTAINER") == "true" 
                       ? "http://file-service:80" 
                       : "http://localhost:5002");
    client.BaseAddress = new Uri(baseUrl);
});

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.MapControllers();

// Health check
app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "API Gateway",
    timestamp = DateTime.UtcNow
}));

app.Run();