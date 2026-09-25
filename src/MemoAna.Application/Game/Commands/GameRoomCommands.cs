using Mediator;
using MemoAna.Application.Common.Abstractions;
using MemoAna.Application.Common.Responses;
using MemoAna.Application.Game.Dtos;
using MemoAna.Application.Game.Requests;

namespace MemoAna.Application.Game.Commands;

public sealed record CreateRoomCommand(CreateRoomRequest Request)
    : ITransactionalRequest<Response<RoomSessionDto>>;

public sealed record JoinRoomCommand(string RoomId, JoinRoomRequest Request)
    : ITransactionalRequest<Response<RoomSessionDto>>;
