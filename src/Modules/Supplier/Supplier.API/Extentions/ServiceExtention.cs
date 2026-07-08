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
    // public static void ConfigureMediatR(this IServiceCollection services)
    // {
    //     services.AddMediatR(cfg =>
    //     {
    //         cfg.RegisterServicesFromAssembly(typeof(UploadUnspscCommand).Assembly);
    //     });
    // }

    /// <summary>
    /// This method is used to inject the entity repository as scoped instance.
    // /// </summary>
    // public static void ConfigureRepositoryWrapper(this IServiceCollection services)
    // {
    //     _ = services.AddScoped<IRepositoryWrapper, RepositoryWrapper>();
    // }
}