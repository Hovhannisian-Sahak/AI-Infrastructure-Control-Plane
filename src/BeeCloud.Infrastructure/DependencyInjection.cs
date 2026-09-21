using BeeCloud.Application.Interfaces;
using BeeCloud.Application.Services;
using BeeCloud.Infrastructure.Persistence;
using BeeCloud.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using BeeCloud.Infrastructure.Redis;
using StackExchange.Redis;

namespace BeeCloud.Infrastructure;

public static class DependencyInjection
{
    public static IServiceCollection AddInfrastructure(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<ApplicationDbContext>(options =>
            options.UseNpgsql(
                configuration.GetConnectionString(
                    "BeeCloudDatabase")));
        var redisConnection =
            configuration.GetConnectionString("Redis");

        services.AddStackExchangeRedisCache(options =>
        {
            options.Configuration = redisConnection;
        });

        services.AddSingleton<IConnectionMultiplexer>(
            _ => ConnectionMultiplexer.Connect(redisConnection!));
        services.AddScoped<IComputeNodeRepository, ComputeNodeRepository>();
        services.AddScoped<IComputeNodeService, ComputeNodeService>();
        services.AddScoped<INetworkRepository, NetworkRepository>();
        services.AddScoped<INetworkService, NetworkService>();
        services.AddScoped<INetworkAttachmentRepository, NetworkAttachmentRepository>();
        services.AddScoped<INetworkAttachmentService, NetworkAttachmentService>();
        services.AddScoped<IHealthCheckRepository, HealthCheckRepository>();
        services.AddScoped<IHealthCheckService, HealthCheckService>();
        services.AddScoped<IIncidentRepository, IncidentRepository>();
        services.AddScoped<IIncidentService, IncidentService>();
        services.AddScoped<INodeSimulationService, NodeSimulationService>();
        services.AddScoped<INodeMetricRepository, NodeMetricRepository>();
        services.AddScoped<INodeMetricService, NodeMetricService>();
        services.AddScoped<IProvisioningQueue, RedisProvisioningQueue>();
        return services;
    }
}