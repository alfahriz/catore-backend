namespace Catore.Backend.Modules.Auth.Public;

public record AuthAccountDto(
    DateTime CreatedOn
);

public record SignUpResultDto(
    bool Success,
    string? ErrorMessage,
    long? UserPk
);

public record LoginResultDto(
    bool Success,
    string? ErrorMessage,
    string? AccessToken,
    string? RefreshToken
);

public record RefreshResultDto(
    bool Success,
    string? ErrorMessage,
    string? AccessToken,
    string? RefreshToken
);

public record ChangePasswordResultDto(
    bool Success,
    string? ErrorMessage
);

public record ResendVerificationResultDto(
    bool Success,
    int CooldownSecondsRemaining
);
