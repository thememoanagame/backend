using FluentValidation;
using MemoAna.Application.Game.Commands;

namespace MemoAna.Application.Game.Validators;

public sealed class CreateRoomCommandValidator : AbstractValidator<CreateRoomCommand>
{
    public CreateRoomCommandValidator()
    {
        RuleFor(x => x.Request.Name).NotEmpty().MaximumLength(100).WithErrorCode("400");
        RuleFor(x => x.Request.ThemeId).NotEmpty().Must(x => Guid.TryParse(x, out _)).WithErrorCode("422");
        RuleFor(x => x.Request.Difficulty).InclusiveBetween(0, 2).WithErrorCode("422");
        RuleFor(x => x.Request.PlayerName).NotEmpty().MaximumLength(50).WithErrorCode("400");

        When(x => x.Request.RequirePassword, () =>
            RuleFor(x => x.Request.Password)
                .NotEmpty().MinimumLength(4).MaximumLength(100)
                .WithErrorCode("400"));
    }
}

public sealed class JoinRoomCommandValidator : AbstractValidator<JoinRoomCommand>
{
    public JoinRoomCommandValidator()
    {
        RuleFor(x => x.RoomId).NotEmpty().Must(x => Guid.TryParse(x, out _)).WithErrorCode("422");
        RuleFor(x => x.Request.PlayerName).NotEmpty().MaximumLength(50).WithErrorCode("400");

        When(x => x.Request.HasPassword, () =>
            RuleFor(x => x.Request.Password).NotEmpty().MaximumLength(100).WithErrorCode("400"));
    }
}
