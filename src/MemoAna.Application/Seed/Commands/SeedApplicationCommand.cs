using Mediator;
using MemoAna.Application.Common.Abstractions;
using MemoAna.Application.Common.Responses;
using MemoAna.Application.Seed.Dtos;

namespace MemoAna.Application.Seed.Commands;

/// <summary>Explicitly initializes the fundamental application data.</summary>
public sealed record SeedApplicationCommand
    : ITransactionalRequest<Response<SeedOperationResultDto>>;
