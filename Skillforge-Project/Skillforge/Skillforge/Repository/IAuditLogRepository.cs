using Skillforge.Dto;
using Skillforge.Domain;

namespace Skillforge.Repository
{
    /// <summary>
    /// Repository interface for immutable audit logs.
    /// Provides filtering and sorting only (no pagination).
    /// </summary>
    public interface IAuditLogRepository
    {
        /// <summary>
        /// Retrieves audit logs based on filters and sorting.
        /// </summary>
        /// <param name="request">Filter + sorting request DTO</param>
        /// <returns>Collection of audit log domain entities</returns>
        Task<IEnumerable<AuditLog>> GetAuditLogsFilteredAsync(AuditLogFilterRequestDto request);
    }
}
