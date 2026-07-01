using MasterData.Application.Contracts.IRepository;
using MasterData.Infrastructure.Persistence;
using MasterData.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;

namespace MasterData.API.Extensions;

public static class ServiceExtensions
{
    public static void ConfigureDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<AppDbContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection")));
    }
    public static void ConfigureRepositoryWrapper(
        this IServiceCollection services)
    {
        services.AddScoped<IUnspscRepository, UnspscRepository>();
        services.AddScoped<IRepositoryWrapper, RepositoryWrapper>();
    }
}