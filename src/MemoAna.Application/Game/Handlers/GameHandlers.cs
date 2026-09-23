using Mediator;
using MemoAna.Application.Common.Responses;
using MemoAna.Application.Game.Abstractions;
using MemoAna.Application.Game.Commands;
using MemoAna.Application.Game.Dtos;
using MemoAna.Application.Game.Queries;

namespace MemoAna.Application.Game.Handlers;

/// <summary>Handles GameData commands and queries through the application service.</summary>
public sealed class GameHandlers(IThemeService themeService)
    : IRequestHandler<CreateGameThemeCommand, Response<GameThemeDto>>,
      IRequestHandler<GetGameThemeByIdQuery, Response<GameThemeDto>>,
      IRequestHandler<GetGameThemeByNameQuery, Response<GameThemeDto>>,
      IRequestHandler<GetGameThemeListQuery, Response<IReadOnlyList<GameThemeDto>>>,    
      IRequestHandler<UpdateGameThemeCommand, Response<GameThemeDto>>,
      IRequestHandler<DeleteGameThemeCommand, Response<bool>>
{
    
    public async ValueTask<Response<bool>> Handle(DeleteGameThemeCommand request, CancellationToken cancellationToken)
    {
        bool result = await themeService.DeleteThemeAsync(request.Id, cancellationToken);
        return result
            ? Response.Success(true)
            : Response.Failure<bool>("Game theme was not found.");
    }

    public async ValueTask<Response<GameThemeDto>> Handle(UpdateGameThemeCommand request, CancellationToken cancellationToken)
    {
        GameThemeDto? data = await themeService.UpdateThemeAsync(request.Id, request.Name, cancellationToken);
        return data is null
            ? Response.Failure<GameThemeDto>("Game theme was not found.")
            : Response.Success(data);
    }

    public async ValueTask<Response<GameThemeDto>> Handle(CreateGameThemeCommand request, CancellationToken cancellationToken)
    {
        GameThemeDto data = await themeService.AddThemeAsync(request.Name, request.LogoStream, request.LogoFilename, request.CardStreams, cancellationToken);
        return data is null ? 
            Response.Failure<GameThemeDto>("Game theme was not created.") :
            Response.Success(data);
    }

    public async ValueTask<Response<IReadOnlyList<GameThemeDto>>> Handle(GetGameThemeListQuery request, CancellationToken cancellationToken)
    {
        return await themeService.FindThemesAsync(x => true, cancellationToken) is IReadOnlyList<GameThemeDto> themes
            ? Response.Success(themes)
            : Response.Failure<IReadOnlyList<GameThemeDto>>("No game themes found.");
    }

    public async ValueTask<Response<GameThemeDto>> Handle(GetGameThemeByNameQuery request, CancellationToken cancellationToken)
    {
        return await themeService.FindThemesAsync(x => x.Name == request.Name, cancellationToken) is IReadOnlyList<GameThemeDto> themes && themes.Count > 0
            ? Response.Success(themes[0])
            : Response.Failure<GameThemeDto>("Game theme was not found.");
    }

    public async ValueTask<Response<GameThemeDto>> Handle(GetGameThemeByIdQuery request, CancellationToken cancellationToken)
    {
        return await themeService.FindThemesAsync(x => x.Id == request.Id, cancellationToken) is IReadOnlyList<GameThemeDto> themes && themes.Count > 0
            ? Response.Success(themes[0])
            : Response.Failure<GameThemeDto>("Game theme was not found.");
    }
}
