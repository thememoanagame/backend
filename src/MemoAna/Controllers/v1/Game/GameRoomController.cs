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
    IGamePublisher gamePublisher) : ControllerBase
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

        return response.Succeeded
            ? StatusCode(StatusCodes.Status201Created, response)
            : BadRequest(response);
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
