using System.Reflection;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.OpenApi.Models;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.AspNetCore.Http.Features;
using NLog.Web;
using Ocelot.DependencyInjection;
using Ocelot.Middleware;
using MMLib.SwaggerForOcelot.DependencyInjection;
using MMLib.SwaggerForOcelot.Middleware;

namespace OcelotGateway
{
    public partial class Program
    {
        protected Program() { }

        public static async Task Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            builder.Host.UseNLog();

            var env = builder.Environment.EnvironmentName;
            IConfiguration configuration = new ConfigurationBuilder()
                .SetBasePath(Directory.GetCurrentDirectory())
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                .AddJsonFile($"appsettings.{env}.json", optional: true, reloadOnChange: true)
                .Build(); 
            
            builder.WebHost.ConfigureKestrel(options =>
            {
                options.Limits.MinRequestBodyDataRate = null;
                options.Limits.MinResponseDataRate = null;
                // Commented out 'Common.MAX_REQUEST_SIZE' because 'Common' is missing in Gateway
                options.Limits.MaxRequestBodySize = 104857600;
            });

            builder.Configuration.AddJsonFile("ocelot.json", optional: false, reloadOnChange: true)
                                 .AddJsonFile("ocelot.SwaggerEndPoints.json", optional: false, reloadOnChange: true);
            builder.Services.AddOcelot(builder.Configuration);
            builder.Services.AddSwaggerForOcelot(builder.Configuration);
           
            // --- COMMENTED OUT: Extension methods not found in OcelotGateway project ---
            // builder.Services.ConfigureRateLimiting();
            // builder.Services.ConfigureCors(configuration);
            // builder.Services.ConfigureDBContext(configuration);
            // builder.Services.ConfigureLoggerService();
            // builder.Services.ConfigureRepositoryWrapper();
            // builder.Services.ConfigureServiceWrapper();
            
            builder.Services.AddHttpClient();
            builder.Services.AddControllers();
            // builder.Services.AddSignalR...

            // --- COMMENTED OUT: KeySpecs is missing in Gateway ---
            // KeySpecs keys = ...

            builder.Services.AddHttpContextAccessor();
            builder.Services.AddMemoryCache();
            builder.Services.Configure<FormOptions>(options =>
            {
               options.MultipartBodyLengthLimit = 104857600;
            });

            builder.Services.AddEndpointsApiExplorer();
            
            var app = builder.Build();

            app.UseForwardedHeaders(new ForwardedHeadersOptions
            {
                ForwardedHeaders = ForwardedHeaders.XForwardedFor | ForwardedHeaders.XForwardedProto,
                KnownNetworks = { },
                KnownProxies = { },
                ForwardLimit = null
            });
            
            app.UseRouting();
            app.UseSwagger();

            app.UseSwaggerForOcelotUI(opt =>
            {
                opt.PathToSwaggerGenerator = "/swagger/docs";
            });

            await app.UseOcelot();
            // app.UseCors("CorsPolicy"); 
            // app.UseMiddleware<CustomExceptionMiddleware>();
            app.UseHttpsRedirection();
            // app.UseAuthentication();
            // app.UseAuthorization();
            app.MapControllers();
          
            app.Run();
        }
    }
}