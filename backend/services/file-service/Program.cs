using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TreeDrive.Infrastructure.Data;
using TreeDrive.Infrastructure.Repositories;
using TreeDrive.FileService.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();

// JWT Authentication
var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Secret"] ?? "SuperSecretKey123!@#$%^&*()ABCDEFGHIJKLMNOPQRSTUVWXYZ123456");
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuerSigningKey = true,
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ValidateIssuer = false,
            ValidateAudience = false,
            ClockSkew = TimeSpan.Zero
        };
    });

// Database
builder.Services.AddSingleton<MongoDbContext>();
builder.Services.AddScoped<FileRepository>();
builder.Services.AddScoped<ShareRepository>(); 
builder.Services.AddScoped<UserRepository>();

// File Storage
builder.Services.AddScoped<IFileStorageService, LocalFileStorageService>();

// CORS
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowAll", policy =>
        policy.AllowAnyOrigin().AllowAnyMethod().AllowAnyHeader());
});

var app = builder.Build();

app.UseHttpsRedirection();
app.UseCors("AllowAll");
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check
app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "File Service",
    timestamp = DateTime.UtcNow
}));

app.Run();