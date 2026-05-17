namespace Skillforge.Dto
{
    /// <summary>
    /// Response DTO for audit logs.
    /// Represents a single immutable audit log entry returned to clients.
    /// </summary>
    public class AuditLogResponseDto
    {
        public int AuditID { get; set; }
        public int? UserID { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Resource { get; set; } = string.Empty;
        public DateTime Timestamp { get; set; }
    }

    /// <summary>
    /// Result wrapper for audit logs.
    /// </summary>
    public class Result<T>
    {
        public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
    }
}
