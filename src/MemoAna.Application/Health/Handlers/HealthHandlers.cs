using Mediator;
using MemoAna.Application.Common.Responses;
using MemoAna.Application.Health.Abstractions;
using MemoAna.Application.Health.Queries;
using MemoAna.Application.Health.Responses;

namespace MemoAna.Application.Health.Handlers;

public class HealthHandlers(IHealthService service) : IRequestHandler<GetApiDiskUsageQuery, Response<HealthCheckResponse>>,
    IRequestHandler<GetHealthCheckQuery, Response<HealthCheckResponse>>,
    IRequestHandler<GetHealthReadyQuery, Response<HealthCheckResponse>>,
    IRequestHandler<GetHostInfoQuery, Response<HealthCheckResponse>>
{
    public async ValueTask<Response<HealthCheckResponse>> Handle(GetApiDiskUsageQuery request, CancellationToken cancellationToken)
         => Response.Success(HealthCheckResponse.FromHealthReport(await service.GetApiDiskUsageAsync(cancellationToken)));

    public async ValueTask<Response<HealthCheckResponse>> Handle(GetHealthCheckQuery request, CancellationToken cancellationToken)
        => Response.Success(HealthCheckResponse.FromHealthReport(await service.GetHealthCheckAsync(cancellationToken)));

    public async ValueTask<Response<HealthCheckResponse>> Handle(GetHealthReadyQuery request, CancellationToken cancellationToken)
        => Response.Success(HealthCheckResponse.FromHealthReport(await service.PerformDbReadinessCheckAsync(cancellationToken)));

    public async ValueTask<Response<HealthCheckResponse>> Handle(GetHostInfoQuery request, CancellationToken cancellationToken)
        => Response.Success(HealthCheckResponse.FromHealthReport(await service.PerformDbReadinessCheckAsync(cancellationToken)));

}
