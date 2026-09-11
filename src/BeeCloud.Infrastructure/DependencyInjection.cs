using BeeCloud.Application.Interfaces;
using BeeCloud.Application.Services;
using BeeCloud.Infrastructure.Persistence;
using BeeCloud.Infrastructure.Persistence.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

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
        services.AddScoped<IComputeNodeRepository, ComputeNodeRepository>();
        services.AddScoped<IComputeNodeService, ComputeNodeService>();
        services.AddScoped<INetworkRepository, NetworkRepository>();
        services.AddScoped<INetworkService, NetworkService>();
        return services;
    }
}