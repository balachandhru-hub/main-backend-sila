using MasterData.Application.Contracts;
using MasterData.Application.Features.ApiClient;
using MasterData.Application.Services;
using MasterData.Domain.Common;
using MasterData.Infrastructure.Contracts.IRepository;
using MasterData.Infrastructure.Contracts.IServices;
using MasterData.Infrastructure.Persistence;
using MasterData.Infrastructure.Repository;
using Microsoft.EntityFrameworkCore;
using SharedKernel.LoggerServices;
using MasterData.Application.Features.Unspsc.Commands;
using MediatR;

namespace MasterData.API.Extensions;

public static class ServiceExtensions
{
    /// <summary>
    /// Configures CORS so the frontend host origin (Origin:HostOriginLocal) can call this API
    /// through the gateway with credentials.
    /// </summary>
    public static void ConfigureCors(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        services.AddCors(options =>
        {
            options.AddPolicy(
                "CorsPolicy",
                builder =>
                    builder
                        .SetIsOriginAllowed(origin =>
                            origin.Equals(
                                configuration[Common.DEFAULT_FRONT_END_ORIGIN_LOCAL],
                                StringComparison.OrdinalIgnoreCase))
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .AllowCredentials()
                        .WithExposedHeaders("Content-Disposition"));
        });
    }

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
        services.AddScoped<IBulkInsertHelper, BulkInsertHelper>();
        services.AddScoped<IUserIdentityService, UserIdentityService>();
        services.AddScoped<IUserContext, UserContext>();
        services.AddScoped<ISupplierSessionApiClient, SupplierSessionApiClient>();
        _ = services.AddControllers();
    }
    public static void ConfigureLoggerService(
    this IServiceCollection services)
    {
        services.AddSingleton<ILoggerManager, LoggerManager>();
    }
    public static void ConfigureMediatR(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(UploadUnspscCommand).Assembly);
        });
    }

    /// <summary>
    /// This method is used to inject the entity repository as scoped instance.
    /// </summary>
    public static void ConfigureRepositoryWrapper(this IServiceCollection services)
    {
        _ = services.AddScoped<IRepositoryWrapper, RepositoryWrapper>();
    }
}