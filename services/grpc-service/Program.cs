using IoTFarmBench.GrpcService.Data;
using IoTFarmBench.GrpcService.Services;
using Microsoft.AspNetCore.Server.Kestrel.Core;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.ConfigureKestrel(options =>
{
    options.ListenAnyIP(5001, listenOptions =>
    {
        listenOptions.Protocols = HttpProtocols.Http2;
    });
});

builder.Services.AddGrpc();
builder.Services.AddGrpcReflection();
builder.Services.AddSingleton<DbConnectionFactory>();
builder.Services.AddScoped<DeviceRepository>();
builder.Services.AddScoped<SensorReadingRepository>();
builder.Services.AddScoped<AnalyticsRepository>();

var app = builder.Build();

app.MapGrpcService<StatusGrpcService>();
app.MapGrpcService<FarmBenchmarkGrpcService>();
app.MapGrpcReflectionService();
app.MapGet("/", () => "IoTFarmBench gRPC service. Use grpcurl -plaintext localhost:5001 list.");

app.Run();
