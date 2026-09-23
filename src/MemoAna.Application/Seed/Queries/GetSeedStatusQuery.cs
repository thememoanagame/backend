using Mediator;
using MemoAna.Application.Common.Responses;
using MemoAna.Application.Seed.Dtos;

namespace MemoAna.Application.Seed.Queries;

/// <summary>Gets the current availability of the seed middleware.</summary>
public sealed record GetSeedStatusQuery
    : IRequest<Response<SeedStatusDto>>;
