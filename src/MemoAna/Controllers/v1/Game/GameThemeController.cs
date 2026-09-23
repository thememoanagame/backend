using Mediator;
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
    [ProducesResponseType(typeof(Application.Common.Responses.Response<IReadOnlyList<GameThemeDto>>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Application.Common.Responses.Response<IReadOnlyList<GameThemeDto>>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Application.Common.Responses.Response<IReadOnlyList<GameThemeDto>>), StatusCodes.Status404NotFound)]
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

    [HttpGet("by-id/{id}")]
    [ProducesResponseType(typeof(Application.Common.Responses.Response<GameThemeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Application.Common.Responses.Response<GameThemeDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Application.Common.Responses.Response<GameThemeDto>), StatusCodes.Status404NotFound)]
    public async ValueTask<IActionResult> GetThemeById(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id))
        {
            return BadRequest(Application.Common.Responses.Response.Failure<GameThemeDto>(["O ID do tema não pode ser vazio."]));
        }

        var query = new GetGameThemeByIdQuery(id);
        var response = await mediator.Send(query, cancellationToken);

        if (response.Succeeded && response.Data != null)
        {
            return Ok(response);
        }

        return NotFound(response);
    }

    [HttpGet("by-name/{name}")]
    [ProducesResponseType(typeof(Application.Common.Responses.Response<GameThemeDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Application.Common.Responses.Response<GameThemeDto>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Application.Common.Responses.Response<GameThemeDto>), StatusCodes.Status404NotFound)]
    public async ValueTask<IActionResult> GetThemeByName(string name, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return BadRequest(Application.Common.Responses.Response.Failure<GameThemeDto>(["O nome do tema não pode ser vazio."]));
        }

        var query = new GetGameThemeByNameQuery(name);
        var response = await mediator.Send(query, cancellationToken);

        if (response.Succeeded && response.Data != null)
        {
            return Ok(response);
        }

        return NotFound(response);
    }
}
