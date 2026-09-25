using Mediator;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MemoAna.Application.Common.Responses;
using MemoAna.Application.Game.Abstractions;
using MemoAna.Application.Game.Commands;
using MemoAna.Application.Game.Dtos;
using MemoAna.Application.Game.Queries;
using MemoAna.Application.Game.Requests;

namespace MemoAna.Controllers.v1.Game;

[ApiController]
[Authorize]
[Route("api/v1/game/rooms")]
public sealed class GameRoomController(
    IMediator mediator,
    IGamePublisher gamePublisher,
    IGameService gameService) : ControllerBase
{
    [HttpGet]
    [ProducesResponseType(typeof(Response<IReadOnlyList<RoomSummaryDto>>), StatusCodes.Status200OK)]
    public async ValueTask<IActionResult> GetRooms(CancellationToken cancellationToken)
    {
        var response = await mediator.Send(new GetGameRoomListQuery(), cancellationToken);
        return Ok(response);
    }

    [HttpPost]
    [ProducesResponseType(typeof(Response<RoomSessionDto>), StatusCodes.Status201Created)]
    public async ValueTask<IActionResult> CreateRoom(
        [FromBody] CreateRoomRequest request,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(
            new CreateRoomCommand(request),
            cancellationToken);

        if (!response.Succeeded || response.Data is null)
            return BadRequest(response);

        if (response.Data.Board is not null)
        {
            await gamePublisher.PublishRoomStartedAsync(
                response.Data,
                cancellationToken);
        }

        return StatusCode(StatusCodes.Status201Created, response);
    }

    [HttpGet("{roomId}/cards/{position:int}/image")]
    [Produces("image/webp", "image/png", "image/jpeg")]
    [ProducesResponseType<FileStreamResult>(StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status401Unauthorized)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async ValueTask<IActionResult> GetCardImage(
        string roomId,
        int position,
        [FromQuery] string token,
        CancellationToken cancellationToken)
    {
        try
        {
            var file = await gameService.GetCardImageAsync(
                roomId,
                position,
                token,
                cancellationToken);

            return File(file.Content, file.ContentType);
        }
        catch (UnauthorizedAccessException)
        {
            return Unauthorized();
        }
        catch (KeyNotFoundException)
        {
            return NotFound();
        }
    }

    [HttpPost("{roomId}/join")]
    [ProducesResponseType(typeof(Response<RoomSessionDto>), StatusCodes.Status200OK)]
    public async ValueTask<IActionResult> JoinRoom(
        string roomId,
        [FromBody] JoinRoomRequest request,
        CancellationToken cancellationToken)
    {
        var response = await mediator.Send(
            new JoinRoomCommand(roomId, request),
            cancellationToken);

        if (!response.Succeeded || response.Data is null)
            return BadRequest(response);

        await gamePublisher.PublishRoomStartedAsync(
            response.Data,
            cancellationToken);

        return Ok(response);
    }
}
