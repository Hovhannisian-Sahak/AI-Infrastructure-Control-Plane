using BeeCloud.Application.Interfaces;
using BeeCloud.Infrastructure.Persistence;
using BeeCloud.Infrastructure.Persistence.Repositories;
using BeeCloud.Worker;
using BeeCloud.Worker.Processors;
using Microsoft.EntityFrameworkCore;

var builder = Host.CreateApplicationBuilder(args);
builder.Services.AddDbContext<ApplicationDbContext>(
    options =>
        options.UseNpgsql(
            builder.Configuration.GetConnectionString(
                "BeeCloudDatabase")));

builder.Services.AddScoped<IComputeNodeRepository, ComputeNodeRepository>();
builder.Services.AddScoped<IHealthCheckRepository, HealthCheckRepository>();
builder.Services.AddHostedService<ProvisioningWorker>();
builder.Services.AddHostedService<HealthMonitoringWorker>();
builder.Services.AddHostedService<RemediationWorker>();
builder.Services.AddScoped<IHealthMonitoringProcessor, HealthMonitoringProcessor>();
builder.Services.AddScoped<IRemediationProcessor, RemediationProcessor>();
builder.Services.AddScoped<IProvisioningProcessor, ProvisioningProcessor>();
var host = builder.Build();
host.Run();
