using Grpc.Core;
using IoTFarmBench.GrpcService.Protos;

namespace IoTFarmBench.GrpcService.Services;

public sealed class StatusGrpcService : StatusService.StatusServiceBase
{
    public override Task<StatusReply> GetStatus(StatusRequest request, ServerCallContext context)
    {
        return Task.FromResult(new StatusReply
        {
            Service = "grpc-service",
            Status = "ok"
        });
    }
}
