
using Contracts.IRepository;
using SharedKernel.ExceptionHandler;
using Microsoft.EntityFrameworkCore;
using Repository;
using Services;
using Quartz;
using Identity.Domain.Common;
using Microsoft.AspNetCore.RateLimiting;
using System.Threading.RateLimiting;
using Identity.Infrastructure.Contracts.IServices;
using Identity.Infrastructure.DbContext;
using SharedKernel.LoggerServices;
using Identity.Application.Services;
using MediatR;
using Identity.Application.Features.Auth.Commands;
using Identity.Application.Features.Auth.Commands.SendEmailVerification;
using Identity.Application.Features.Auth.Commands.VerifyOtp;
using HashingSystem;
using Identity.Application.Contracts;
using Identity.Infrastructure.ApiClients;


namespace Identity.API.Extensions
{
    /// <summary>
    /// Class <c>Service Extenstions</c> is static consists of service extensions.
    /// Contains static method to configure the services required to run the application.
    /// </summary>
    public static class ServiceExtensions
    {
        /// <summary>
        /// This method is used to configure the CORS (Cross-Origin Resource Sharing).
        /// CORS is a mechanism that gives rights to the user to access resources
        /// from the server on a different domain
        /// </summary>
        public static void ConfigureCors(this IServiceCollection services, IConfiguration config)
        {
            _ = services.AddCors(options =>
            {
                options.AddPolicy(
                    "CorsPolicy",
                    builder =>
                        builder
                            .SetIsOriginAllowed(origin =>

                        origin.Equals(config[Common.DEFAULT_FRONT_END_ORIGIN_LOCAL]!, StringComparison.OrdinalIgnoreCase)
                   
                    )
                            .AllowAnyMethod()
                            .AllowAnyHeader()
                            .AllowCredentials()
                            .WithExposedHeaders("Content-Disposition")
                );
            });
        }
        /// <summary>
        /// This method is used to configure the rate limiting for the APIs. It limits the number of requests from a single IP address to prevent abuse and protect the server from overload.
        /// </summary> <param name="services"></param>
        public static void ConfigureRateLimiting(this IServiceCollection services)
        {
            // Forwarded Headers (for real IP behind proxy)
            services.Configure<ForwardedHeadersOptions>(options =>
            {
                options.ForwardedHeaders =
                    Microsoft.AspNetCore.HttpOverrides.ForwardedHeaders.XForwardedFor;
            });

            // Rate Limiter
            services.AddRateLimiter(options =>
            {
                options.AddPolicy("LoginPolicy", httpContext =>
                    RateLimitPartition.GetFixedWindowLimiter(
                       partitionKey: httpContext.Request.Headers["X-Forwarded-For"].FirstOrDefault()
                            ?? httpContext.Connection.RemoteIpAddress?.ToString()
                            ?? "unknown",
                        factory: _ => new FixedWindowRateLimiterOptions
                        {
                            PermitLimit = 5,
                            Window = TimeSpan.FromMinutes(1),
                            QueueProcessingOrder = QueueProcessingOrder.OldestFirst,
                            QueueLimit = 0
                        }));

                options.OnRejected = async (context, token) =>
                {
                    context.HttpContext.Response.StatusCode = 429;
                    context.HttpContext.Response.ContentType = "application/json";

                    var response = new
                    {
                        status_code = 429,
                        message = "Maximum limit reached. Please try again later.",
                        description = "Too many requests from this IP. Please try again later."
                    };

                    await context.HttpContext.Response.WriteAsJsonAsync(response);
                };
            });
        }
        /// <summary>
        /// This method is used to configure the IIS integration helps to configure the
        /// properties for IIS server deployment.
        /// </summary>
        public static void ConfigureIISIntegration(this IServiceCollection services)
        {
            _ = services.Configure<IISOptions>(options => { });
        }

        /// <summary>
        /// This method is used to inject logger service inside the .NET Core’s IOC container with
        /// singleton scope.
        /// </summary>
        public static void ConfigureLoggerService(this IServiceCollection services)
        {
            _ = services.AddSingleton<ILoggerManager, LoggerManager>();
        }

        /// <summary>
        /// This method is used to inject the repository context into IOC with DBContext scope.
        /// </summary>
        /// <paramref name="config">The config paramter used to read attributes in appsettings.json</paramref>
        public static void ConfigureDBContext(
            this IServiceCollection services,
            IConfiguration config
        )
        {
            string dbString = config.GetConnectionString("DefaultConnection")!;
            services.AddDbContext<RepositoryContext>(options =>
            {
                options.UseSqlServer(dbString);
            });
        }

        /// <summary>
        /// This method is used to inject the services where the business logics are implemented.
        /// </summary>
        public static void ConfigureServiceWrapper(
            this IServiceCollection services
        )
        {
         
            _ = services.AddScoped<IUserIdentityService, UserIdentityService>();
            _ = services.AddScoped<IUserContext, UserContext>();
            _ = services.AddScoped<IBcryptHashing, BcryptHashing>();
             _ = services.AddScoped<IBuyerApiClient, BuyerApiClient>();
            _ = services.AddScoped<ISupplierApiClient, SupplierApiClient>();

            _ = services.AddControllers();

        }

        /// <summary>
        /// This method is used to inject the entity repository as scoped instance.
        /// </summary>
        public static void ConfigureRepositoryWrapper(this IServiceCollection services)
        {
            _ = services.AddScoped<IRepositoryWrapper, RepositoryWrapper>();
        }

        public static void ConfigureScheduler(this IServiceCollection services)
        {
   
        
        }

        /// <summary>
        /// This method is used to inject the mappings of Entities and Dto.
        /// </summary>
   

        /// <summary>
        /// This method is used to inject the custom exception middleware.
        /// </summary>
        public static IApplicationBuilder UseHttpStatusCodeExceptionMiddleware(
            this IApplicationBuilder builder
        )
        {
            return builder.UseMiddleware<CustomExceptionMiddleware>();
        }
            public static void ConfigureMediatR(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssembly(typeof(SendEmailVerificationCommand).Assembly);
            cfg.RegisterServicesFromAssembly(typeof(VerifyOtpCommand).Assembly);
        });
    }

      
    }
}
