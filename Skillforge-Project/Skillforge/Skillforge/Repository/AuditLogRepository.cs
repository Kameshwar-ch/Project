using Microsoft.EntityFrameworkCore;
using Skillforge.Data;
using Skillforge.Domain;
using Skillforge.Dto;

namespace Skillforge.Repository
{
    /// <summary>
    /// EF Core implementation of IAuditLogRepository.
    /// Applies filters and sorting using enums.
    /// Timestamp filter: date-only OR exact timestamp
    /// Only date provided → match all logs for that day
    /// </summary>
    public class AuditLogRepository : IAuditLogRepository
    {
        private readonly SkillForgeDB _context;
        public AuditLogRepository(SkillForgeDB context)
        {
            _context = context;
        }

        public async Task<IEnumerable<AuditLog>> GetAuditLogsFilteredAsync(AuditLogFilterRequestDto request)
        {
            IQueryable<AuditLog> query = _context.AuditLogs.AsQueryable();

            if (request.AuditID.HasValue)
                query = query.Where(x => x.AuditID == request.AuditID.Value);

            if (request.UserID.HasValue)
                query = query.Where(x => x.UserID == request.UserID.Value);

            if (!string.IsNullOrWhiteSpace(request.Resource))
                query = query.Where(x => EF.Functions.Like(x.Resource, $"%{request.Resource}%"));

            if (!string.IsNullOrWhiteSpace(request.Action))
                query = query.Where(x => EF.Functions.Like(x.Action, $"%{request.Action}%"));

            if (request.Timestamp.HasValue)
            {
                var ts = request.Timestamp.Value;

                if (ts.TimeOfDay == TimeSpan.Zero)
                {
                    query = query.Where(x => x.Timestamp.Date == ts.Date);
                }
                else
                {
                    query = query.Where(x => x.Timestamp == ts);
                }
            }

            query = request.SortBy switch
            {
                SortBy.AuditID   => request.SortOrder == SortOrder.asc ? query.OrderBy(x => x.AuditID)   : query.OrderByDescending(x => x.AuditID),
                SortBy.UserID    => request.SortOrder == SortOrder.asc ? query.OrderBy(x => x.UserID)    : query.OrderByDescending(x => x.UserID),
                SortBy.Resource  => request.SortOrder == SortOrder.asc ? query.OrderBy(x => x.Resource)  : query.OrderByDescending(x => x.Resource),
                SortBy.Action    => request.SortOrder == SortOrder.asc ? query.OrderBy(x => x.Action)    : query.OrderByDescending(x => x.Action),
                SortBy.Timestamp => request.SortOrder == SortOrder.asc ? query.OrderBy(x => x.Timestamp) : query.OrderByDescending(x => x.Timestamp),
                _ => query.OrderByDescending(x => x.Timestamp)
            };

            return await query.ToListAsync();
        }
    }
}
