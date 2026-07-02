// using TicketSystemAPI;
// using Entities;
// using ExceptionHandler;
using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
// using TicketSystemAPI.Extensions;
// using Microsoft.OpenApi.Models; // Commented out to fix CS0246 OpenApiInfo error
// using HashingSystem;
// using Entities.Common;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http.Features;
using NLog;
using NLog.Web;

namespace Supplier.API
{
    public partial class Program
    {
        protected Program() { }

        public static void Main(string[] args)
        {
            // Early init of NLog to catch setup errors
            var logger = LogManager.Setup().LoadConfigurationFromFile("nlog.config").GetCurrentClassLogger();
            logger.Debug("init main");

            try
            {
                var builder = WebApplication.CreateBuilder(args);

                // Setup NLog as the logging provider
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

                    // Default fallback value if common settings are stripped out
                    options.Limits.MaxRequestBodySize = 104857600;
                });

                // --- COMMENTED OUT UNWANTED SERVICES ---
                // builder.Services.ConfigureRateLimiting();
                // builder.Services.ConfigureCors(configuration);
                builder.Services.AddCors(options =>
                {
                    options.AddPolicy("CorsPolicy", policy =>
                    {
                        policy.AllowAnyOrigin()
                              .AllowAnyHeader()
                              .AllowAnyMethod();
                    });
                });
                // builder.Services.ConfigureDBContext(configuration);
                // builder.Services.ConfigureLoggerService(); 
                // builder.Services.ConfigureRepositoryWrapper();
                // builder.Services.ConfigureAutoMapper();
                // builder.Services.ConfigureAuthentication();
                // builder.Services.ConfigureServiceWrapper();
                
                builder.Services.AddControllers(); 
                builder.Services.AddHttpClient();
                // builder.Services.AddSignalR(options => { options.EnableDetailedErrors = true; });

                // KeySpecs keys = new KeySpecs()
                // {
                //     Salt = configuration["Hashing:Salt"],
                //     WorkFactor = Int32.TryParse(configuration["Hashing:WorkFactor"], out int numValue) ? numValue : 11
                // };
                // builder.Services.RegisterHashing(keys);

                builder.Services.AddHttpContextAccessor();
                builder.Services.AddMemoryCache();
                builder.Services.Configure<FormOptions>(options =>
                {
                    options.MultipartBodyLengthLimit = 104857600; 
                });

                // --- COMMENTED OUT TO FIX CS0246 OPENAPIINFO ERROR ---
                // builder.Services.AddEndpointsApiExplorer();
                // builder.Services.AddSwaggerGen(c =>
                // {
                //     c.SwaggerDoc("v1.0", new OpenApiInfo
                //     {
                //         Title = "Supplier APIs",
                //         Version = "v1.0",
                //         Description = "REST APIs"
                //     });
                //     var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                //     var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                //     if (File.Exists(xmlPath)) 
                //     {
                //         c.IncludeXmlComments(xmlPath);
                //     }
                // });
                // builder.Services.ConfigureScheduler();

                var app = builder.Build();

                app.Logger.LogInformation("ProcurementSuite Supplier API started successfully!");

                // --- COMMENTED OUT UNWANTED DB MIGRATION / SEEDING ---
                // using (var scope = app.Services.CreateScope())
                // {
                //     DBMigration.UpdateDatabase(scope.ServiceProvider);
                //     SeedData.Initialize(scope.ServiceProvider);
                // }

                app.UseForwardedHeaders(new ForwardedHeadersOptions
                {
                    ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
                    KnownNetworks = { },
                    KnownProxies = { },
                    ForwardLimit = null
                });

                // --- COMMENTED OUT SWAGGER MIDDLEWARE ---
                // if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == "UAT")
                // {
                //     app.UseSwagger();
                //     app.UseSwaggerUI(c =>
                //     {
                //         c.SwaggerEndpoint("/swagger/v1.0/swagger.json", "Supplier API v1.0");
                //         c.RoutePrefix = "swagger";
                //     });
                // }

                app.UseRouting();
                app.UseCors("CorsPolicy");
                // app.UseMiddleware<CustomExceptionMiddleware>();
                // app.UseRateLimiter();

                if (!app.Environment.IsDevelopment())
                {
                    app.UseHttpsRedirection(); 
                }

                // app.UseAuthentication();
                // app.UseAuthorization();
                app.MapControllers();
                // app.MapHub<TicketMessageHub>("/ticket-message-hub").RequireCors("CorsPolicy");

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