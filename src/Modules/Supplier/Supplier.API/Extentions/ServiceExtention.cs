using Supplier.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;
using SharedKernel.LoggerServices;



namespace Supplier.API.Extensions;

public static class ServiceExtensions
{
    public static void ConfigureDatabase(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddDbContext<RepositoryContext>(options =>
            options.UseSqlServer(
                configuration.GetConnectionString("DefaultConnection")));
    }

    public static void ConfigureServiceWrapper(
            this IServiceCollection services)
    {
    
        _ = services.AddControllers();
    }
    public static void ConfigureLoggerService(
    this IServiceCollection services)
    {
        services.AddSingleton<ILoggerManager, LoggerManager>();
    }
   
}