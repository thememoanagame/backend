using MemoAna.Application.Common.Responses;
using MemoAna.Application.Identity.Responses;
using Mediator;

namespace MemoAna.Application.Identity.Queries;

/// <summary>Requests identity information.</summary>
/// <param name="UserId">The user identifier.</param>
public sealed record GetIdentityInfoQuery(
    string UserId)
    : IRequest<Response<IdentityInfoResponse>>;
