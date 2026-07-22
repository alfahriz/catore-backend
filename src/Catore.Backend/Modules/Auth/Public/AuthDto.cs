namespace Catore.Backend.Modules.Auth.Public;

public record AuthAccountDto(
    DateTime CreatedOn
);

public record SignUpResultDto(
    bool Success,
    string? ErrorMessage,
    Guid? UserAccountPk
);

public record LoginResultDto(
    bool Success,
    string? ErrorMessage,
    string? AccessToken,
    string? RefreshToken
);
