using Mediator;
using MemoAna.Application.Common.Responses;
using MemoAna.Application.Health.Responses;

namespace MemoAna.Application.Health.Queries;

public sealed record GetHealthReadyQuery : IRequest<Response<HealthCheckResponse>>;
