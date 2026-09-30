using Supplier.Infrastructure.DbContext;
using Microsoft.EntityFrameworkCore;
using SharedKernel.LoggerServices;
using Supplier.Infrastructure.Contracts.IServices;
using Supplier.Application.Services;
using Supplier.Application.Features.Commands.Supplier;
using Supplier.Infrastructure.Contracts.IRepository;
using Supplier.Infrastructure.Repository;
using Supplier.Domain.Common;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System.IdentityModel.Tokens.Jwt;
using Supplier.Infrastructure.ApiClients;
using Supplier.Application.Contracts;
using HashingSystem;
using SharedKernel.Contracts;





namespace Supplier.API.Extensions

{
    public static class ServiceExtensions
    {
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
            services.AddScoped<IUserIdentityService, UserIdentityService>();
            services.AddScoped<IUserContext, UserContext>();
            services.AddScoped<ISessionTokenValidator, SupplierSessionTokenValidator>();
            _ = services.AddScoped<IMetadataApiClient, MetadataApiClient>();
            _ = services.AddScoped<IIdentityApiClient, IdentityApiClient>();
            _ =services.AddScoped<IBuyerApiClient, BuyerApiClient>();
            _ =services.AddScoped<IBcryptHashing,BcryptHashing>();
           
            _ = services.AddControllers();
        }
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
        public static void ConfigureLoggerService(
        this IServiceCollection services)
        {
            services.AddSingleton<ILoggerManager, LoggerManager>();
        }
        public static void ConfigureMediatR(this IServiceCollection services)
        {
            services.AddMediatR(cfg =>
            {
                cfg.RegisterServicesFromAssembly(typeof(CreateSupplierProfileCommand).Assembly);
            });
        }
        /// <summary>
        /// This method is used to inject the entity repository as scoped instance.
        /// </summary>
        public static void ConfigureRepositoryWrapper(this IServiceCollection services)
        {
            _ = services.AddScoped<IRepositoryWrapper, RepositoryWrapper>();
            _ = services.AddScoped<Supplier.Application.Services.ISupplierPurchaseOrderService, Supplier.Application.Services.SupplierPurchaseOrderService>();
        }
        public static void ConfigureAuthentication(
               this IServiceCollection services
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
                        IssuerSigningKey = new SymmetricSecurityKey(
                            Encoding.UTF8.GetBytes("1kt6ryCXoY13_9LrCfyfF_wSgPVxlewGtrBlY0PkyxA")
                        ),
                        ValidateLifetime = false,
                        ClockSkew = TimeSpan.Zero //the default for this setting is 5 minutes
                    };

                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            if (context.Request.Cookies.TryGetValue(Common.COOKIE_ACCESS_TOKEN_KEY, out var token))
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

