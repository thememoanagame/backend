using Mediator;
using MemoAna.Application.Common.Responses;
using MemoAna.Application.Game.Dtos;
using MemoAna.Application.Game.Queries;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace MemoAna.Controllers.v1.Game;

[Route("api/v1/game/themes")]
[ApiController]
public class GameThemeController(IMediator mediator) : ControllerBase
{
    [HttpGet]
    [ResponseCache(Duration = 300)]
    [ProducesResponseType(typeof(Application.Common.Responses.Response<IReadOnlyCollection<GameThemeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Application.Common.Responses.Response<IReadOnlyCollection<GameThemeDto>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Application.Common.Responses.Response<IReadOnlyCollection<GameThemeDto>>), StatusCodes.Status404NotFound)]
    public async ValueTask<IActionResult> GetThemes(CancellationToken cancellationToken)
    {
        var query = new GetGameThemeListQuery();
        var response = await mediator.Send(query, cancellationToken);

        if (response is not { Succeeded: true, Data: { Count: > 0 } })
        {
            return NotFound(response);
        }

        if (response.Succeeded)
        {
            return Ok(response);
        }

        return BadRequest(response);
    }

    [HttpGet("{key}/theme")]
    [ProducesResponseType(typeof(Application.Common.Responses.Response<GameThemeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Application.Common.Responses.Response<GameThemeDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Application.Common.Responses.Response<GameThemeDto>), StatusCodes.Status404NotFound)]
    public async ValueTask<IActionResult> Get(string key, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(key) || string.IsNullOrEmpty(key))
        {
            return BadRequest(Application.Common.Responses.Response.Failure<GameThemeDto>(["O filtro de busca do tema não pode ser vazio."]));
        }

        var isId = Guid.TryParse(key, out var _);
        Application.Common.Responses.Response<GameThemeDto> response;
        if (isId)
        {
            response = await mediator.Send(new GetGameThemeByIdQuery(key), cancellationToken);
        }
        else
        {
            response = await mediator.Send(new GetGameThemeByNameQuery(key), cancellationToken);
        }

        if (response.Succeeded && response.Data != null)
        {
            return Ok(response);
        }

        return NotFound(response);
    }

}
