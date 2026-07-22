using SharedKernel.ExceptionHandler;
using System.Reflection;
using Microsoft.OpenApi.Models;
using Buyer.Domain.Common;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http.Features;
using Buyer.Infrastructure;
using Buyer.API.Extensions;

namespace Buyer.API
{
    public partial class Program
    {
        protected Program() { }

        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


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
            builder.Services.ConfigureAuthentication();
            builder.Services.ConfigureMediatR();
            builder.Services.AddHttpClient();
            builder.Services.AddSignalR(options =>
            {
                options.EnableDetailedErrors = true;
            });




            builder.Services.AddHttpContextAccessor();
            builder.Services.AddMemoryCache();

            builder.Services.Configure<FormOptions>(options =>
            {
                options.MultipartBodyLengthLimit = 104857600; // Set the maximum request body size to 100 MB 
            });

            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "Buyer System APIs",
        Version = "v1",
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
                // SeedData.Initialize(scope.ServiceProvider);
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
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Buyer System API's v1");
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
    }
}