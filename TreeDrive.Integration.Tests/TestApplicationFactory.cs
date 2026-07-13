using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using DotNet.Testcontainers.Containers;
using DotNet.Testcontainers.Builders;
using Xunit;

namespace TreeDrive.Integration.Tests;

public class TestApplicationFactory : WebApplicationFactory<Program>, IAsyncLifetime
{
    private readonly IContainer _mongoDbContainer;
    private string _connectionString = string.Empty;

    public TestApplicationFactory()
    {
        _mongoDbContainer = new ContainerBuilder()
            .WithImage("mongo:latest")
            .WithPortBinding(27017, true)
            .WithWaitStrategy(Wait.ForUnixContainer().UntilPortIsAvailable(27017))
            .Build();
    }

    public async Task InitializeAsync()
    {
        await _mongoDbContainer.StartAsync();
        _connectionString = $"mongodb://localhost:{_mongoDbContainer.GetMappedPublicPort(27017)}";
    }

    async Task IAsyncLifetime.DisposeAsync()
    {
        await _mongoDbContainer.DisposeAsync();
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.ConfigureAppConfiguration((context, config) =>
        {
            var settings = new Dictionary<string, string?>
            {
                ["MongoDB:ConnectionString"] = _connectionString,
                ["MongoDB:DatabaseName"] = "TreeDriveTest"
            };
            
            config.AddInMemoryCollection(settings!);
        });
    }
}