using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;
using Testcontainers.PostgreSql;

namespace BeeCloud.IntegrationTests.Infrastructure;

public sealed class PostgreSqlTestContainer : IAsyncDisposable
{
    private readonly PostgreSqlContainer _container =
        new PostgreSqlBuilder()
            .WithImage("postgres:16")
            .WithDatabase("beecloud_test")
            .WithUsername("beecloud_test")
            .WithPassword("beecloud_test_password")
            .Build();

    public string ConnectionString =>
        _container.GetConnectionString();

    public async Task StartAsync()
    {
        await _container.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}