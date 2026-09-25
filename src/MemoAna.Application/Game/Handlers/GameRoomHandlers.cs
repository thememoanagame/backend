using Mediator;
using MemoAna.Application.Common.Responses;
using MemoAna.Application.Game.Abstractions;
using MemoAna.Application.Game.Commands;
using MemoAna.Application.Game.Dtos;
using MemoAna.Application.Game.Queries;

namespace MemoAna.Application.Game.Handlers;

public sealed class GameRoomHandlers(IGameService gameService)
    : IRequestHandler<CreateRoomCommand, Response<RoomSessionDto>>,
      IRequestHandler<JoinRoomCommand, Response<RoomSessionDto>>,
      IRequestHandler<GetGameRoomListQuery, Response<IReadOnlyList<RoomSummaryDto>>>
{
    public async ValueTask<Response<RoomSessionDto>> Handle(CreateRoomCommand request, CancellationToken cancellationToken)
        => Response.Success(await gameService.CreateRoomAsync(request.Request, cancellationToken));

    public async ValueTask<Response<RoomSessionDto>> Handle(JoinRoomCommand request, CancellationToken cancellationToken)
        => Response.Success(await gameService.JoinRoomAsync(request.RoomId, request.Request, cancellationToken));

    public async ValueTask<Response<IReadOnlyList<RoomSummaryDto>>> Handle(GetGameRoomListQuery request, CancellationToken cancellationToken)
        => Response.Success(await gameService.ListRoomsAsync(cancellationToken));
}
