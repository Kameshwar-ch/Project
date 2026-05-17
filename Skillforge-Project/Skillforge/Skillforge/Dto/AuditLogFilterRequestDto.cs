namespace Skillforge.Dto
{
    /// <summary>
    /// Enum for allowed sort orders.
    /// Swagger will render this as a dropdown (asc/desc).
    /// </summary>
    public enum SortOrder
    {
        asc,
        desc
    }

    /// <summary>
    /// Enum for allowed sort fields.
    /// Swagger will render this as a dropdown (AuditID, UserID, Resource, Action, Timestamp).
    /// </summary>
    public enum SortBy
    {
        AuditID,
        UserID,
        Resource,
        Action,
        Timestamp
    }

    /// <summary>
    /// Request DTO for filtering and sorting audit logs.
    /// Pagination has been removed — only filters and sorting remain.
    /// </summary>
    public class AuditLogFilterRequestDto
    {
        // Filter conditions
        public int? AuditID { get; set; }
        public int? UserID { get; set; }
        public string? Resource { get; set; }
        public string? Action { get; set; }
        public DateTime? Timestamp { get; set; }
        public SortBy SortBy { get; set; } = SortBy.Timestamp;
        public SortOrder SortOrder { get; set; } = SortOrder.desc;
    }
}
