using Mediator;
using MemoAna.Application.Common.Abstractions;
using MemoAna.Application.Common.Responses;
using MemoAna.Application.Game.Dtos;
using MemoAna.Domain.Game;

namespace MemoAna.Application.Game.Commands;
 
/// <summary>Creathes a game theme.</summary>
public record CreateGameThemeCommand(
    string Name,
    Stream LogoStream,
    string LogoFilename,
    IReadOnlyList<(string Filename, Stream Stream)> CardStreams
) : ITransactionalRequest<Response<GameThemeDto>>;

/// <summary>Updates a game theme.</summary>
public record UpdateGameThemeCommand(
    string Id,
    string Name
) : ITransactionalRequest<Response<GameThemeDto>>;

/// <summary>Deletes a game theme.</summary>
public record DeleteGameThemeCommand(
    string Id
) : ITransactionalRequest<Response<bool>>;
