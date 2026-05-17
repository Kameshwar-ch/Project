using System;

namespace Skillforge.Utility;

public static class ResetPasswordUtility
{
    public const string EmailRequired = "Email is required.";
    public const string EnterPassword = "New password is required.";
    public const string NoMatch = "Passwords do not match.";
    public const string Updated = "Password updated successfully.";
    public const string Failed = "Something went wrong. Please try again.";
    public const string NoUppercaseInPassword = "The Password must have atleast one uppercase Character";
    public const string NoNumericInPassword = "The Password must contain atleast one Numeric Character";
    public const string PasswordLength = "The Password must have atleast 8 Characters";
}
