using SharedKernel.ExceptionHandler;
using Microsoft.EntityFrameworkCore;
using Operations.Domain.Common;
using System.Threading.RateLimiting;
using System.Text.Json.Serialization;
using Operations.Infrastructure.Contracts.IServices;
using Operations.Infrastructure.DbContext;
using SharedKernel.LoggerServices;
using Operations.Application.Services;
using Operations.Application.Services.Graph;
using Operations.Application.Services.Integration;
using Operations.Application.Services.Ocr;
using Operations.Application.Services.Storage;
using Operations.Application.Services.Workers;
using Operations.Application.Features.Queries.GetInvoices;
using Operations.Infrastructure.Contracts.IRepository;
using Operations.Infrastructure.Repository;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.DataProtection;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Operations.API.Extensions
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
            this IServiceCollection services,
            IConfiguration config
        )
        {
            _ = services.AddScoped<IUserIdentityService, UserIdentityService>();
            _ = services.AddScoped<IUserContext, UserContext>();

            // Technical engines used by the handlers (no use-case logic lives in them).
            _ = services.AddScoped<IDocumentStorageService, LocalDocumentStorageService>();
            _ = services.AddSingleton<IOcrProvider, BuiltInOcrProvider>();
            _ = services.AddScoped<IInvoiceOcrPipeline, InvoiceOcrPipeline>();
            _ = services.AddScoped<IMicrosoftGraphClient, MicrosoftGraphClient>();
            _ = services.AddScoped<IIntegrationCredentialProtector, IntegrationCredentialProtector>();
            _ = services.AddScoped<IIntegrationHttpExecutor, IntegrationHttpExecutor>();
            _ = services.AddScoped<IIntegrationSpreadsheetEngine, IntegrationSpreadsheetEngine>();

            _ = services.AddHttpClient(Common.HTTP_CLIENT_GRAPH, client => client.Timeout = TimeSpan.FromSeconds(30));
            _ = services.AddHttpClient(Common.HTTP_CLIENT_EXTRACTION, client => client.Timeout = TimeSpan.FromSeconds(30));
            _ = services.AddHttpClient(Common.HTTP_CLIENT_INTEGRATIONS);

            // ERP credentials are encrypted with a key ring that must survive a restart of the
            // container, so it is kept next to the documents instead of in the user profile.
            string basePath = string.IsNullOrWhiteSpace(config[Common.BASE_FOLDER_PATH]) ? AppContext.BaseDirectory : config[Common.BASE_FOLDER_PATH]!;
            _ = services.AddDataProtection()
                .SetApplicationName("ProcurementSuite.Operations")
                .PersistKeysToFileSystem(new DirectoryInfo(Path.Combine(basePath, Common.DATA_PROTECTION_SUBFOLDER)));

            // Background workers. Each one dispatches MediatR requests inside its own DI scope.
            _ = services.AddSingleton<IInvoiceProcessingQueue, InvoiceProcessingQueue>();
            _ = services.AddHostedService<InvoiceProcessingWorker>();
            _ = services.AddHostedService<DocumentTransferWorker>();
            _ = services.AddHostedService<IntegrationSchedulerWorker>();

            // Enums travel as their names and timestamps as UTC, as the Operations screens expect.
            _ = services.AddControllers().AddJsonOptions(options =>
            {
                options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
                options.JsonSerializerOptions.Converters.Add(new UtcDateTimeJsonConverter());
            });
        }

        /// <summary>
        /// This method is used to inject the entity repository as scoped instance.
        /// </summary>
        public static void ConfigureRepositoryWrapper(this IServiceCollection services)
        {
            _ = services.AddScoped<IRepositoryWrapper, RepositoryWrapper>();
        }

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
                cfg.RegisterServicesFromAssembly(typeof(GetInvoicesQuery).Assembly);
            });
        }

        public static void ConfigureAuthentication(
               this IServiceCollection services,
               IConfiguration config
           )
        {
            services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = false;
                    options.SaveToken = true;

                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateAudience = false,
                        ValidateIssuer = false,
                        ValidateIssuerSigningKey = true,

                        // The signing key comes from configuration (Tokens:Key), the same key the
                        // [ApiAuthorization] attribute validates the access_token cookie with.
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes(config[Common.TOKEN_KEY] ?? string.Empty)
                        ),
                        ValidateLifetime = false,
                        ClockSkew = TimeSpan.Zero //the default for this setting is 5 minutes
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            if (context.Request.Cookies.TryGetValue(Common.ACCESS_TOKEN, out var token))
                            {
                                context.Token = token;
                            }
                            return Task.CompletedTask;
                        },
                        OnAuthenticationFailed = context =>
                        {
                            if (
                                context.Exception.GetType() == typeof(SecurityTokenExpiredException)
                            )
                            {
                                context.Response.Headers.Append("Token-Expired", "true");
                            }
                            return Task.CompletedTask;
                        }
                    };
                });
        }
    }
}
