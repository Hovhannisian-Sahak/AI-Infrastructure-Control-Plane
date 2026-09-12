using BeeCloud.Application.Interfaces;
using BeeCloud.Infrastructure.Persistence;
using BeeCloud.Infrastructure.Persistence.Repositories;
using BeeCloud.Worker;
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
var host = builder.Build();
host.Run();
