using Skillforge.Dto;

namespace Skillforge.Service
{
    /// <summary>
    /// Service interface for audit log operations.
    /// Provides filtering and sorting functionality.
    /// </summary>
    public interface IAuditLogService
    {
        /// <summary>
        /// Retrieves audit logs based on filters and sorting.
        /// </summary>
        /// <param name="request">Filter + sorting request DTO</param>
        /// <returns>Result wrapper containing response DTOs</returns>
        Task<Result<AuditLogResponseDto>> GetAuditLogsAsync(AuditLogFilterRequestDto request);
    }
}
