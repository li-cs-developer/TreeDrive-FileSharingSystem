using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Networks;
using Xunit;

namespace TreeDrive.Integration.Tests;

public class TestApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly INetwork _network;
    private readonly IContainer _mongoDbContainer;
    private readonly IContainer _authServiceContainer;
    private string _connectionString = string.Empty;

    public TestApplicationFactory()
    {
        // ✅ Create a shared network
        _network = new NetworkBuilder()
            .WithName($"treedrive-test-network-{Guid.NewGuid():N}")
            .Build();

        // MongoDB container with network alias
        _mongoDbContainer = new ContainerBuilder()
            .WithImage("mongo:latest")
            .WithNetwork(_network)
            .WithNetworkAliases("mongodb")  // ✅ This is the hostname other containers use
            .WithPortBinding(27017, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(27017))
            .Build();

        // Auth Service container - uses the network alias "mongodb" for connection
        _authServiceContainer = new ContainerBuilder()
            .WithImage("treedrive-auth-service:latest")
            .WithNetwork(_network)
            .WithPortBinding(5001, true)
            .WithEnvironment("ASPNETCORE_ENVIRONMENT", "Testing")
            .WithEnvironment("ASPNETCORE_URLS", "http://+:5001")
            // ✅ Use the network alias "mongodb" instead of host.docker.internal
            .WithEnvironment("MongoDB__ConnectionString", "mongodb://mongodb:27017")
            .WithEnvironment("Jwt__Secret", "SuperSecretKey123!@#$%^&*()ABCDEFGHIJKLMNOPQRSTUVWXYZ123456")
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilPortIsAvailable(5001)
                .UntilMessageIsLogged("Application started"))
            .Build();
    }

    public async Task InitializeAsync()
    {
        // Start MongoDB
        await _mongoDbContainer.StartAsync();
        _connectionString = $"mongodb://localhost:{_mongoDbContainer.GetMappedPublicPort(27017)}";
        
        // Start Auth Service
        await _authServiceContainer.StartAsync();
        
        // Wait for services to be ready
        await Task.Delay(3000);
        
        // Get the port for the API Gateway to use
        var authServicePort = _authServiceContainer.GetMappedPublicPort(5001);
        Console.WriteLine($"Auth Service running on port: {authServicePort}");
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _authServiceContainer.DisposeAsync();
        await _mongoDbContainer.DisposeAsync();
        await _network.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var authServicePort = _authServiceContainer.GetMappedPublicPort(5001);
            
            var settings = new Dictionary<string, string?>
            {
                ["MongoDB:ConnectionString"] = _connectionString,
                ["MongoDB:DatabaseName"] = "TreeDriveTest",
                ["Services:AuthService"] = $"http://localhost:{authServicePort}",
                ["Services:FileService"] = "http://localhost:5002"
            };
            
            config.AddInMemoryCollection(settings!);
        });
    }
}