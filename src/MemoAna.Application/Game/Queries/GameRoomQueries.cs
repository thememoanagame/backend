using Mediator;
using MemoAna.Application.Common.Responses;
using MemoAna.Application.Game.Dtos;

namespace MemoAna.Application.Game.Queries;

public sealed record GetGameRoomListQuery
    : IRequest<Response<IReadOnlyList<RoomSummaryDto>>>;
