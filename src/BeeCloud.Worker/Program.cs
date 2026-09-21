using BeeCloud.Application.Interfaces;
using BeeCloud.Application.Services;
using BeeCloud.Infrastructure.Persistence;
using BeeCloud.Infrastructure.Persistence.Repositories;
using BeeCloud.Infrastructure.Redis;
using BeeCloud.Worker;
using BeeCloud.Worker.Processors;
using BeeCloud.Worker.Workers;
using Microsoft.EntityFrameworkCore;
using StackExchange.Redis;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddDbContext<ApplicationDbContext>(
    options =>
        options.UseNpgsql(
            builder.Configuration.GetConnectionString(
                "BeeCloudDatabase")));
var redisConnection =
    builder.Configuration.GetConnectionString("Redis");

builder.Services.AddStackExchangeRedisCache(options =>
{
    options.Configuration = redisConnection;
});

builder.Services.AddSingleton<IConnectionMultiplexer>(
    _ => ConnectionMultiplexer.Connect(redisConnection!));
builder.Services.AddScoped<IIncidentRepository, IncidentRepository>();
builder.Services.AddScoped<IIncidentService, IncidentService>();

builder.Services.AddScoped<IComputeNodeRepository, ComputeNodeRepository>();
builder.Services.AddScoped<IHealthCheckRepository, HealthCheckRepository>();
builder.Services.AddScoped<INodeMetricRepository, NodeMetricRepository>();

builder.Services.AddScoped<IProvisioningQueue, RedisProvisioningQueue>();

builder.Services.AddScoped<IHealthMonitoringProcessor, HealthMonitoringProcessor>();
builder.Services.AddScoped<IRemediationProcessor, RemediationProcessor>();
builder.Services.AddScoped<IProvisioningProcessor, ProvisioningProcessor>();
builder.Services.AddScoped<IMetricsProcessor, MetricsProcessor>();

builder.Services.AddHostedService<ProvisioningWorker>();
builder.Services.AddHostedService<HealthMonitoringWorker>();
builder.Services.AddHostedService<RemediationWorker>();
builder.Services.AddHostedService<MetricsWorker>();

var host = builder.Build();
host.Run();
