using IoTFarmBench.RestService.Infrastructure.Database;

namespace IoTFarmBench.RestService.Api.Endpoints;

public static class HealthEndpoints
{
    public static IEndpointRouteBuilder MapHealthEndpoints(this IEndpointRouteBuilder app)
    {
        app.MapGet("/health", (DatabaseOptions database) =>
        {
            return Results.Ok(new
            {
                service = "rest-service",
                status = "ok",
                database = database.Host
            });
        })
        .WithName("Health");

        return app;
    }
}
