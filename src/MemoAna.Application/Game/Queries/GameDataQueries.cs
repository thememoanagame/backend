using Mediator;
using MemoAna.Application.Common.Responses;
using MemoAna.Application.Game.Dtos;

namespace MemoAna.Application.Game.Queries;

/// <summary>Gets a game theme by its identifier.</summary>
public sealed record GetGameThemeByIdQuery(string Id)
    : IRequest<Response<GameThemeDto>>;

/// <summary>Gets a game theme by its name.</summary>
public sealed record GetGameThemeByNameQuery(string Name)
    : IRequest<Response<GameThemeDto>>;

/// <summary>Lists all game themes available to the game.</summary>
public sealed record GetGameThemeListQuery 
    : IRequest<Response<IReadOnlyList<GameThemeDto>>>;
