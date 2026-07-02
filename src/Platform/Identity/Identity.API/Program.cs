using TicketSystemAPI;
using Entities;
using ExceptionHandler;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using TicketSystemAPI.Extensions;
using Microsoft.OpenApi.Models;
using HashingSystem;
using Identity.Domain.Common;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http.Features;
using NLog;
using NLog.Web;

namespace TicketSystemAPI
{
    public partial class Program
    {
        protected Program() { }

        public static void Main(string[] args)
        {
            var logger = LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();
            logger.Debug("init main");

            try
            {
                var builder = WebApplication.CreateBuilder(args);

                builder.Logging.ClearProviders();
                builder.Host.UseNLog();

                var env = builder.Environment.EnvironmentName;
                IConfiguration configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true)
                    .Build();
                builder.WebHost.ConfigureKestrel(options =>
                {
                    // Disable the minimum data rate limits for requests and responses
                    options.Limits.MinRequestBodyDataRate = null;
                    options.Limits.MinResponseDataRate = null;
                    if (long.TryParse(configuration[Common.MAX_REQUEST_SIZE], out long maxSize))
                    {
                        options.Limits.MaxRequestBodySize = maxSize;
                    }
                    else
                    {
                        options.Limits.MaxRequestBodySize = 104857600;
                    }
                });
           
                builder.Services.ConfigureRateLimiting();
                builder.Services.ConfigureCors(configuration);
                builder.Services.ConfigureDBContext(configuration);
                builder.Services.ConfigureLoggerService();
                builder.Services.ConfigureRepositoryWrapper();
                builder.Services.ConfigureServiceWrapper();
                builder.Services.AddHttpClient();
                builder.Services.AddSignalR(options =>
                {
                    options.EnableDetailedErrors = true;
                });


                KeySpecs keys = new KeySpecs()
                {
                    Salt = configuration["Hashing:Salt"],
                    WorkFactor = Int32.TryParse(configuration["Hashing:WorkFactor"], out int numValue) ? numValue : 11
                };
                // builder.Services.RegisterHashing(keys);

                builder.Services.AddHttpContextAccessor();
                builder.Services.AddMemoryCache();
                builder.Services.Configure<FormOptions>(options =>
                {
                   options.MultipartBodyLengthLimit = 104857600; // Set the maximum request body size to 100 MB 
                });

                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1.0", new OpenApiInfo
                    {
                        Title = "TICKET System APIs",
                        Version = "v1.0",
                        Description = "REST APIs"
                    });


                    // Set the comments path for the Swagger JSON and UI.
                    var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                    var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                    c.IncludeXmlComments(xmlPath);
                });
                builder.Services.ConfigureScheduler();
                var app = builder.Build();

                using (var scope = app.Services.CreateScope())
                {
                    DBMigration.UpdateDatabase(scope.ServiceProvider);

                }

                app.UseForwardedHeaders(new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,

                    // Required for LB / Docker / Cloud
                    KnownNetworks = { },
                    KnownProxies = { },
                    ForwardLimit = null
                });

                // Configure the HTTP request pipeline
                if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == Common.UAT_ENVIRONMENT)
                {
                    app.UseSwagger();
                    app.UseSwaggerUI(c =>
                    {
                        c.SwaggerEndpoint("/swagger/v1.0/swagger.json", "Ticket System API's v1.0");
                        c.RoutePrefix = "swagger";
                    });
                }
                app.UseRouting();
                app.UseCors("CorsPolicy");
                app.UseMiddleware<CustomExceptionMiddleware>();
                app.UseRateLimiter();
                app.UseHttpsRedirection();
                app.UseAuthentication();
                app.UseAuthorization();
                app.MapControllers();

              
                app.Run();
            }
            catch (Exception exception)
            {
                logger.Error(exception, "Stopped program because of exception");
                throw;
            }
            finally
            {
                LogManager.Shutdown();
            }
        }
    }
}
