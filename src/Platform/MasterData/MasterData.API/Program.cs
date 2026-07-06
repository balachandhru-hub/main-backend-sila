using System.Reflection;
using SharedKernel.ExceptionHandler;
using MasterData.API.Extensions;
using Microsoft.AspNetCore.Http.Features;
using Microsoft.AspNetCore.HttpOverrides;
using Microsoft.OpenApi.Models;
using MasterData.Domain.Common;
using MasterData.Infrastructure;
using MasterData.Infrastructure.Persistence;

namespace MasterData.API;

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

        builder.Services.AddControllers();

        builder.Services.ConfigureDatabase(configuration);
        builder.Services.ConfigureServiceWrapper();
        builder.Services.ConfigureRepositoryWrapper();


        builder.Services.ConfigureMediatR();
        builder.Services.AddHttpClient();

        builder.Services.AddHttpContextAccessor();
        builder.Services.ConfigureLoggerService();

        builder.Services.AddMemoryCache();

        builder.Services.Configure<FormOptions>(options =>
        {
            options.MultipartBodyLengthLimit = 104857600;
        });



        builder.Services.AddEndpointsApiExplorer();

        builder.Services.AddSwaggerGen(c =>
        {
            c.SwaggerDoc("v1", new OpenApiInfo
            {
                Title = "MasterData APIs",
                Version = "v1",
                Description = "REST APIs"
            });

            var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
            var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
            c.IncludeXmlComments(xmlPath);

        });

        var app = builder.Build();

        // Database Migration & Seed Data
        using (var scope = app.Services.CreateScope())
        {
            DBMigration.UpdateDatabase(scope.ServiceProvider);
            SeedData.Initialize(scope.ServiceProvider);
        }

        // Forwarded Headers
        app.UseForwardedHeaders(new ForwardedHeadersOptions
        {
            ForwardedHeaders = ForwardedHeaders.XForwardedFor |
                               ForwardedHeaders.XForwardedProto,

            KnownNetworks = { },
            KnownProxies = { },
            ForwardLimit = null
        });

        if (app.Environment.IsDevelopment() || app.Environment.EnvironmentName == Common.UAT_ENVIRONMENT)
        {
            app.UseSwagger();

            app.UseSwaggerUI(c =>
            {
                c.SwaggerEndpoint("/swagger/v1/swagger.json", "MasterData APIs v1");
                c.RoutePrefix = "swagger";
            });
        }
        app.UseRouting();
        app.UseCors("CorsPolicy");
        app.UseHttpsRedirection();

        app.UseMiddleware<CustomExceptionMiddleware>();

        app.UseAuthentication();
        app.UseAuthorization();
        app.MapControllers();

        app.Run();
    }
}