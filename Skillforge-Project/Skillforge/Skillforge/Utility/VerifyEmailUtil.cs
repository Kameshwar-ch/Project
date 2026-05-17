using System;

namespace Skillforge.Utility;

public static class VerifyEmailUtility
{
    public const string EmailRequired = "Email is required.";
    public const string EmailNotFound = "No account found with this email.";
    public const string EmailFound = "Email verified. You can now reset your password.";
}
