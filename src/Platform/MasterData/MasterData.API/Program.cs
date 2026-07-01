using MasterData.API.Extensions;
using MasterData.Application.Features.Unspsc.Commands;
using MediatR;
var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.ConfigureDatabase(builder.Configuration);
builder.Services.ConfigureRepositoryWrapper();

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddMediatR(cfg =>
{
    cfg.RegisterServicesFromAssembly(typeof(UploadUnspscCommand).Assembly);
});

var app = builder.Build();



app.UseHttpsRedirection();

app.UseAuthorization();

app.MapControllers();

app.Run();