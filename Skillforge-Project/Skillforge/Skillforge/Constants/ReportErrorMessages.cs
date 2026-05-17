namespace Skillforge.Constants;

/// <summary>
/// Centralised error message strings for report scheduling business rule violations.
/// </summary>
public static class ReportErrorMessages
{
    public const string InvalidCronExpression = "Invalid cron expression. Example: \"0 9 * * 1\" for every Monday at 9 AM.";
    public const string InvalidTokenClaims = "Invalid token claims.";
}
