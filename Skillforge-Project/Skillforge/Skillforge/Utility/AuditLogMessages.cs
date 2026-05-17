namespace Skillforge.Utility
{
    /// <summary>
    /// Centralized messages for audit log operations.
    /// Ensures consistent responses across controller, service, and repository.
    /// </summary>
    public static class AuditLogMessages
    {
        public const string NotFound = "Audit log not found.";
        public const string NoLogs = "No audit logs available.";
        public const string InvalidId = "Invalid audit log ID.";

        // Sorting validation messages (aligned with enums in DTO)
        public const string InvalidSortBy = "Invalid sort field. Allowed: AuditID, UserID, Resource, Action, Timestamp.";
        public const string InvalidSortOrder = "Invalid sort order. Allowed: asc, desc.";

        // General operation messages
        public const string Error = "An unexpected error occurred while retrieving audit logs.";
        public const string Success = "Audit logs retrieved successfully.";
    }
}
