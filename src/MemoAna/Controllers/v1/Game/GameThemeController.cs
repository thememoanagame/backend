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

    [HttpGet("{id}/cards")]
    [ProducesResponseType(typeof(Application.Common.Responses.Response<FilesDto>), StatusCodes.Status200OK)]
    [ProducesResponseType(typeof(Application.Common.Responses.Response<string>), StatusCodes.Status400BadRequest)]
    [ProducesResponseType(typeof(Application.Common.Responses.Response<string>), StatusCodes.Status404NotFound)]
    public async ValueTask<IActionResult> GetThemeCards(string id, CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(id) || string.IsNullOrEmpty(id))
        {
            return BadRequest(Application.Common.Responses.Response.Failure<string>(["O id do tema não pode ser vazio."]));
        }
        if (!Guid.TryParse(id, out var _))
        {
            return BadRequest(Application.Common.Responses.Response.Failure<string>(["O id do tema deve ser um GUID válido."]));
        }

        var query = new GetGameThemeCardsQuery(id);
        Response<FilesDto> response = await mediator.Send(query, cancellationToken);

        if (response.Succeeded && response.Data != null)
        {
            return Ok(response);
        }

        return NotFound(response);
    }

    /// <summary>
    /// Gets the image of a specific card from a theme.
    /// </summary>
    /// <param name="id">The ID of the theme</param>
    /// <param name="cardId">The ID of the card</param>
    /// <param name="cancellationToken">The cancellation token</param>
    /// <returns>The image of the card</returns>
    [HttpGet("{id}/cards/{cardId}")]
    [Produces("image/webp", "image/png", "image/jpeg")]
    [ProducesResponseType<FileStreamResult>(StatusCodes.Status200OK)]
    [ProducesResponseType<Response<FileDto>>(StatusCodes.Status400BadRequest)]
    [ProducesResponseType<Response<FileDto>>(StatusCodes.Status404NotFound)]
    public async ValueTask<IActionResult> GetThemeCardImage(string id, string cardId, CancellationToken cancellationToken)
    {
        if (!Guid.TryParse(id, out var _) || !Guid.TryParse(cardId, out var _))
        {
            return BadRequest(Application.Common.Responses.Response.Failure<string>(["Os identificadores do tema e da carta devem ser GUIDs válidos."]));
        }

        var query = new GetThemeCardsByIdQuery(id, cardId);
        Response<FileDto> response = await mediator.Send(query, cancellationToken);

        if (response.Succeeded && response.Data != null)
        {
            // Retorna o FileStreamResult nativo do ASP.NET Core. 
            // Ele lê a stream, envia ao cliente via HTTP e faz o Dispose da stream e da conexão com segurança.
            return File(response.Data.Content, response.Data.ContentType);
        }

        // Se o Mediator retornou false, significa que não achou no LiteDB
        return NotFound(response);
    }
}
