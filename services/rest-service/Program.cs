using IoTFarmBench.RestService.Api.Endpoints;
using IoTFarmBench.RestService.Data;
using IoTFarmBench.RestService.Infrastructure.Database;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();
builder.Services.AddSingleton<DatabaseOptions>();
builder.Services.AddSingleton<DbConnectionFactory>();
builder.Services.AddScoped<DeviceRepository>();
builder.Services.AddScoped<SensorReadingRepository>();
builder.Services.AddScoped<AnalyticsRepository>();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.MapControllers();
app.MapHealthEndpoints();

app.MapGet("/", () => Results.Redirect("/swagger"))
    .ExcludeFromDescription();

app.Run();
