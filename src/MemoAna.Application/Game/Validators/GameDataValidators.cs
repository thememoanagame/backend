using FluentValidation;
using MemoAna.Application.Common.Abstractions;
using MemoAna.Application.Common.Responses;
using MemoAna.Application.Game.Commands;
using MemoAna.Application.Game.Queries;

namespace MemoAna.Application.Game.Validators;


public class CreateGameThemeCommandValidator : AbstractValidator<CreateGameThemeCommand>
{
    private static readonly string[] AllowedExtensions = [".webp"];
    private const long MaxFileSizeInBytes = 7 * 1024 * 1024;
    public CreateGameThemeCommandValidator()
    {
        // 1. Validação do Nome do Tema
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Theme name is required.").WithErrorCode("400")
            .MaximumLength(100).WithMessage("Theme name must not exceed 100 characters.").WithErrorCode("400");

        // 2. Validação do Arquivo de Logo (Filename e Stream)
        RuleFor(x => x.LogoFilename)
            .NotEmpty().WithMessage("Logo filename is required.").WithErrorCode("400")
            .Must(BeAValidImageExtension).WithMessage("Logo must be a valid image file (.jpg, .png, .webp).").WithErrorCode("400");

        RuleFor(x => x.LogoStream)
            .NotNull().WithMessage("Logo stream is required.").WithErrorCode("400")
            .Must(stream => stream.Length > 0).WithMessage("Logo file cannot be empty.").WithErrorCode("400")
            .Must(stream => stream.Length <= MaxFileSizeInBytes).WithMessage("Logo file must be under 7MB.").WithErrorCode("400")
            .Must(BeAValidImageStream).WithMessage("Logo file is corrupted or not a valid image format.").WithErrorCode("400");

        // 3. Validação da Lista de Cartas (CardStreams)
        RuleFor(x => x.CardStreams)
            .NotNull().WithMessage("Card streams are required.").WithErrorCode("400")
            // Mínimo de 15 itens
            .Must(cards => cards != null && cards.Count == 15)
            .WithMessage("You must provide exactly 15 cards.").WithErrorCode("400")
            // Sem arquivos com nomes repetidos na lista
            .Must(HaveUniqueFilenames)
            .WithMessage("Card filenames must be unique within the theme. No duplicates allowed.").WithErrorCode("400");

        // 4. Validação individual de cada Carta dentro da lista de tuplas
        RuleForEach(x => x.CardStreams).ChildRules(card =>
        {
            card.RuleFor(c => c.Filename)
                .NotEmpty().WithMessage("Card filename is required.").WithErrorCode("400")
                .Must(BeAValidImageExtension).WithMessage("Card must be a valid image file.").WithErrorCode("400");

            card.RuleFor(c => c.Stream)
                .NotNull().WithMessage("Card stream is required.").WithErrorCode("400")
                .Must(stream => stream != null && stream.Length > 0).WithMessage("Card stream cannot be empty.").WithErrorCode("400")
                .Must(stream => stream != null && stream.Length <= MaxFileSizeInBytes).WithMessage("Card file must be under 7MB.").WithErrorCode("400")
                .Must(BeAValidImageStream).WithMessage("Card file is corrupted or not a valid image format.").WithErrorCode("400");
        });
    }

    private bool HaveUniqueFilenames(System.Collections.Generic.IReadOnlyList<(string Filename, Stream Stream)> cards)
    {
        if (cards == null) return false;
        // Verifica se a contagem de nomes distintos é igual ao total de itens da lista
        var distinctNames = cards.Select(c => c.Filename.ToLowerInvariant()).Distinct().Count();
        return distinctNames == cards.Count;
    }

    private bool BeAValidImageExtension(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename)) return false;
        var ext = Path.GetExtension(filename).ToLowerInvariant();
        return AllowedExtensions.Contains(ext);
    }

    private bool BeAValidImageStream(Stream stream)
    {
        if (stream is null || !stream.CanRead || !stream.CanSeek || stream.Length < 12)
            return false;

        var originalPosition = stream.Position;
        stream.Position = 0;

        try
        {
            Span<byte> header = stackalloc byte[12];
            var read = stream.Read(header);

            return read == 12
                && header[0] == (byte)'R'
                && header[1] == (byte)'I'
                && header[2] == (byte)'F'
                && header[3] == (byte)'F'
                && header[8] == (byte)'W'
                && header[9] == (byte)'E'
                && header[10] == (byte)'B'
                && header[11] == (byte)'P';
        }
        finally
        {
            stream.Position = originalPosition;
        }
    }
 FluentValidation;
using MemoAna.Application.Common.Abstractions;
using MemoAna.Application.Common.Responses;
using MemoAna.Application.Game.Commands;
using MemoAna.Application.Game.Queries;

namespace MemoAna.Application.Game.Validators;


public class CreateGameThemeCommandValidator : AbstractValidator<CreateGameThemeCommand>
{
    private static readonly string[] AllowedExtensions = [".webp"];
    private const long MaxFileSizeInBytes = 7 * 1024 * 1024;
    public CreateGameThemeCommandValidator()
    {
        // 1. Validação do Nome do Tema
        RuleFor(x => x.Name)
            .NotEmpty().WithMessage("Theme name is required.").WithErrorCode("400")
            .MaximumLength(100).WithMessage("Theme name must not exceed 100 characters.").WithErrorCode("400");

        // 2. Validação do Arquivo de Logo (Filename e Stream)
        RuleFor(x => x.LogoFilename)
            .NotEmpty().WithMessage("Logo filename is required.").WithErrorCode("400")
            .Must(BeAValidImageExtension).WithMessage("Logo must be a valid image file (.jpg, .png, .webp).").WithErrorCode("400");

        RuleFor(x => x.LogoStream)
            .NotNull().WithMessage("Logo stream is required.").WithErrorCode("400")
            .Must(stream => stream.Length > 0).WithMessage("Logo file cannot be empty.").WithErrorCode("400")
            .Must(stream => stream.Length <= MaxFileSizeInBytes).WithMessage("Logo file must be under 7MB.").WithErrorCode("400")
            .Must(BeAValidImageStream).WithMessage("Logo file is corrupted or not a valid image format.").WithErrorCode("400");

        // 3. Validação da Lista de Cartas (CardStreams)
        RuleFor(x => x.CardStreams)
            .NotNull().WithMessage("Card streams are required.").WithErrorCode("400")
            // Mínimo de 15 itens
            .Must(cards => cards != null && cards.Count == 15)
            .WithMessage("You must provide exactly 15 cards.").WithErrorCode("400")
            // Sem arquivos com nomes repetidos na lista
            .Must(HaveUniqueFilenames)
            .WithMessage("Card filenames must be unique within the theme. No duplicates allowed.").WithErrorCode("400");

        // 4. Validação individual de cada Carta dentro da lista de tuplas
        RuleForEach(x => x.CardStreams).ChildRules(card =>
        {
            card.RuleFor(c => c.Filename)
                .NotEmpty().WithMessage("Card filename is required.").WithErrorCode("400")
                .Must(BeAValidImageExtension).WithMessage("Card must be a valid image file.").WithErrorCode("400");

            card.RuleFor(c => c.Stream)
                .NotNull().WithMessage("Card stream is required.").WithErrorCode("400")
                .Must(stream => stream != null && stream.Length > 0).WithMessage("Card stream cannot be empty.").WithErrorCode("400")
                .Must(stream => stream != null && stream.Length <= MaxFileSizeInBytes).WithMessage("Card file must be under 7MB.").WithErrorCode("400")
                .Must(BeAValidImageStream).WithMessage("Card file is corrupted or not a valid image format.").WithErrorCode("400");
        });
    }

    private bool HaveUniqueFilenames(System.Collections.Generic.IReadOnlyList<(string Filename, Stream Stream)> cards)
    {
        if (cards == null) return false;
        // Verifica se a contagem de nomes distintos é igual ao total de itens da lista
        var distinctNames = cards.Select(c => c.Filename.ToLowerInvariant()).Distinct().Count();
        return distinctNames == cards.Count;
    }

    private bool BeAValidImageExtension(string filename)
    {
        if (string.IsNullOrWhiteSpace(filename)) return false;
        var ext = Path.GetExtension(filename).ToLowerInvariant();
        return AllowedExtensions.Contains(ext);
    }

    private bool BeAValidImageStream(Stream stream)
    {
        if (stream == null || !stream.CanRead || !stream.CanSeek || stream.Length < 4)
            return false;
        // Guarda a posição original para não quebrar a leitura posterior do arquivo
        var originalPosition = stream.Position;
        stream.Position = 0;
        try
        {
            // Lê os primeiros bytes (Magic Numbers / File Signatures) para descobrir o formato real
            byte[] header = new byte[4];
            stream.Read(header, 0, 4);

            // Verifica as assinaturas de arquivos comuns
            bool isJpeg = header[0] == 0xFF && header[1] == 0xD8;
            bool isPng = header[0] == 0x89 && header[1] == 0x50 && header[2] == 0x4E && header[3] == 0x47;
            bool isWebp = header[0] == 0x52 && header[1] == 0x49; // "RI" de RIFF (WebP começa com RIFF)
            bool isGif = header[0] == 0x47 && header[1] == 0x49 && header[2] == 0x46; // "GIF"

            return isJpeg || isPng || isWebp || isGif;
        }
        finally
        {
            // Retorna o stream para a posição original
            stream.Position = originalPosition;
        }
    }
}

public sealed class DeleteGameThemeCommandValidator : AbstractValidator<DeleteGameThemeCommand>
{
    public DeleteGameThemeCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Theme ID is required.")
            .WithErrorCode("400");
        RuleFor(x => x.Id)
            .Must(id => Guid.TryParse(id, out _))
            .WithMessage("Theme ID must be a valid GUID.")
            .WithErrorCode("422");
    }
}

public sealed class UpdateGameThemeCommandValidator : AbstractValidator<UpdateGameThemeCommand>
{
    public UpdateGameThemeCommandValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Theme ID is required.")
            .WithErrorCode("400");
        RuleFor(x => x.Id)
            .Must(id => Guid.TryParse(id, out _))
            .WithMessage("Theme ID must be a valid GUID.")
            .WithErrorCode("422");
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Theme name is required.")
            .WithErrorCode("422")
            .MaximumLength(100)
            .WithMessage("Theme name must not exceed 100 characters.")
            .WithErrorCode("422");
    }
}

public class GetGameThemeByIdQueryValidator : AbstractValidator<GetGameThemeByIdQuery>
{
    public GetGameThemeByIdQueryValidator()
    {
        RuleFor(x => x.Id)
            .NotEmpty()
            .WithMessage("Theme ID is required.")
            .WithErrorCode("400");
    }
}

public class GetGameThemeByNameQueryValidator : AbstractValidator<GetGameThemeByNameQuery>
{
    public GetGameThemeByNameQueryValidator()
    {
        RuleFor(x => x.Name)
            .NotEmpty()
            .WithMessage("Theme name is required.")
            .WithErrorCode("400");
    }
}
