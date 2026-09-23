namespace MemoAna.Application.Seed.Dtos;

/// <summary>Describes whether the application is ready to leave seed mode.</summary>
public sealed record SeedStatusDto(
    bool ApplicationUserExists,
    bool RolesExist,
    bool AdministratorRoleLinked,
    bool SeedRequired,
    IReadOnlyList<string> MissingRequirements);

/// <summary>Describes the changes made by an explicit seed operation.</summary>
public sealed record SeedOperationResultDto(
    bool UserCreated,
    IReadOnlyList<string> RolesCreated,
    SeedStatusDto FinalStatus);
