using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using TreeDrive.Infrastructure.Data;
using TreeDrive.Infrastructure.Repositories;
using TreeDrive.Infrastructure.Helpers;

var builder = WebApplication.CreateBuilder(args);

// ✅ Make sure this is present
builder.Services.AddControllers();

// ❌ REMOVE Swagger (optional)
// builder.Services.AddEndpointsApiExplorer();
// builder.Services.AddSwaggerGen();

// JWT Authentication
var key = Encoding.ASCII.GetBytes(builder.Configuration["Jwt:Secret"] ?? "SuperSecretKey123!@#$%^&*()ABCDEFGHIJKLMNOPQRSTUVWXYZ");
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
builder.Services.AddScoped<UserRepository>();

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

// ✅ This is the most important line - it maps your controllers
app.MapControllers();

// Health check
app.MapGet("/health", () => Results.Ok(new
{
    status = "Healthy",
    service = "Auth Service",
    timestamp = DateTime.UtcNow
}));

app.Run();