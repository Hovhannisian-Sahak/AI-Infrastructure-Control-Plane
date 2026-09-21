using DotNet.Testcontainers.Builders;
using DotNet.Testcontainers.Containers;

namespace BeeCloud.IntegrationTests.Infrastructure;

public class RedisTestContainer : IAsyncDisposable
{
    private readonly IContainer _container;

    public string ConnectionString =>
        $"{_container.Hostname}:{_container.GetMappedPublicPort(6379)}";

    public RedisTestContainer()
    {
        _container =
            new ContainerBuilder()
                .WithImage("redis:7")
                .WithPortBinding(6379, true)
                .WithWaitStrategy(
                    Wait.ForUnixContainer()
                        .UntilInternalTcpPortIsAvailable(6379))
                .Build();
    }

    public async Task StartAsync()
    {
        await _container.StartAsync();
    }

    public async ValueTask DisposeAsync()
    {
        await _container.DisposeAsync();
    }
}