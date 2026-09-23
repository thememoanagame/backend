using Mediator;
using MemoAna.Application.Common.Responses;
using MemoAna.Application.Seed.Abstractions;
using MemoAna.Application.Seed.Commands;
using MemoAna.Application.Seed.Dtos;
using MemoAna.Application.Seed.Queries;

namespace MemoAna.Application.Seed.Handlers;

/// <summary>Handles the seed middleware commands and queries.</summary>
public sealed class SeedHandlers(ISqlSeedService seedService)
    : IRequestHandler<GetSeedStatusQuery, Response<SeedStatusDto>>,
      IRequestHandler<SeedApplicationCommand, Response<SeedOperationResultDto>>
{
    /// <inheritdoc />
    public async ValueTask<Response<SeedStatusDto>> Handle(
        GetSeedStatusQuery request,
        CancellationToken cancellationToken)
    {
        return Response.Success(
            await seedService.GetStatusAsync(cancellationToken));
    }

    /// <inheritdoc />
    public async ValueTask<Response<SeedOperationResultDto>> Handle(
        SeedApplicationCommand request,
        CancellationToken cancellationToken)
    {
        return Response.Success(
            await seedService.SeedAsync(cancellationToken));
    }
}
