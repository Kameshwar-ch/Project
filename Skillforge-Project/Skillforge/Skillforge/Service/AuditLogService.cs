using Skillforge.Dto;
using Skillforge.Repository;

namespace Skillforge.Service
{
    /// <summary>
    /// Service implementation for audit log operations.
    /// Maps domain entities into response DTOs.
    /// </summary>
    public class AuditLogService : IAuditLogService
    {
        private readonly IAuditLogRepository _repository;

        public AuditLogService(IAuditLogRepository repository)
        {
            _repository = repository;
        }

        public async Task<Result<AuditLogResponseDto>> GetAuditLogsAsync(AuditLogFilterRequestDto request)
        {
            var logs = await _repository.GetAuditLogsFilteredAsync(request);

            var responseItems = logs.Select(log => new AuditLogResponseDto
            {
                AuditID = log.AuditID,
                UserID = log.UserID,
                Action = log.Action,
                Resource = log.Resource,
                Timestamp = log.Timestamp
            });

            return new Result<AuditLogResponseDto>
            {
                Items = responseItems
            };
        }
    }
}
