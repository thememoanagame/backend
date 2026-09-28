using Mediator;
using MemoAna.Application.Common.Responses;
using MemoAna.Application.Game.Abstractions;
using MemoAna.Application.Game.Commands;
using MemoAna.Application.Game.Dtos;
using MemoAna.Application.Game.Queries;

namespace MemoAna.Application.Game.Handlers;

/// <summary>Handles GameTheme commands and queries through the application service.</summary>
public sealed class GameThemeHandlers(IThemeService themeService, IGameService gameService)
    : IRequestHandler<CreateGameThemeCommand, Response<GameThemeDto>>,
      IRequestHandler<DeleteGameThemeCommand, Response<bool>>,
      IRequestHandler<GetGameThemeByIdQuery, Response<GameThemeDto>>,
      IRequestHandler<GetGameThemeByNameQuery, Response<GameThemeDto>>,
      IRequestHandler<GetGameThemeListQuery, Response<IReadOnlyList<GameThemeDto>>>,
      IRequestHandler<GameImageCardQuery, FileDto>,    
      IRequestHandler<UpdateGameThemeCommand, Response<GameThemeDto>>
{
    /// <inheritdoc />
    public async ValueTask<Response<GameThemeDto>> Handle(CreateGameThemeCommand request, CancellationToken cancellationToken)
    {
        GameThemeDto data = await themeService.AddThemeAsync(request.Name, request.LogoStream, request.LogoFilename, request.CardStreams, cancellationToken);
        return data is null ?
            ResponseMaker.Failure<GameThemeDto>("Game theme was not created.") :
            ResponseMaker.Success(data);
    }

    /// <inheritdoc />
    public async ValueTask<Response<bool>> Handle(DeleteGameThemeCommand request, CancellationToken cancellationToken)
    {
        bool result = await themeService.DeleteThemeAsync(request.Id, cancellationToken);
        return result
            ? ResponseMaker.Success(true)
            : ResponseMaker.Failure<bool>("Game theme was not found.");
    }

    /// <inheritdoc />
    public async ValueTask<Response<GameThemeDto>> Handle(GetGameThemeByIdQuery request, CancellationToken cancellationToken)
    {
        return await themeService.FindThemesAsync(x => x.Id == request.Id, cancellationToken) is IEnumerable<GameThemeDto> themes && themes.Any()
            ? ResponseMaker.Success(themes.First())
            : ResponseMaker.Failure<GameThemeDto>("Game theme was not found.");
    }

    /// <inheritdoc />
    public async ValueTask<Response<GameThemeDto>> Handle(GetGameThemeByNameQuery request, CancellationToken cancellationToken)
    {
        return await themeService.FindThemesAsync(x => x.Name == request.Name, cancellationToken) is IEnumerable<GameThemeDto> themes && themes.Any()
            ? ResponseMaker.Success(themes.First())
            : ResponseMaker.Failure<GameThemeDto>("Game theme was not found.");
    }

    /// <inheritdoc />
    public async ValueTask<Response<IReadOnlyList<GameThemeDto>>> Handle(GetGameThemeListQuery request, CancellationToken cancellationToken)
    {
        var themes = (await themeService.FindThemesAsync(x => true, cancellationToken)).ToList();
        return ResponseMaker.Success<IReadOnlyList<GameThemeDto>>(themes);
    }

    /// <inheritdoc />
    public async ValueTask<Response<GameThemeDto>> Handle(UpdateGameThemeCommand request, CancellationToken cancellationToken)
    {
        GameThemeDto? data = await themeService.UpdateThemeAsync(request.Id, request.Name, cancellationToken);
        return data is null
            ? ResponseMaker.Failure<GameThemeDto>("Game theme was not found.")
            : ResponseMaker.Success(data);
    }

    public async ValueTask<FileDto> Handle(GameImageCardQuery request, CancellationToken cancellationToken)
    { 
        return await gameService.GetCardImageAsync(
            request.RoomId,
            request.Position,
            request.Token,
            cancellationToken); 
    }
}
