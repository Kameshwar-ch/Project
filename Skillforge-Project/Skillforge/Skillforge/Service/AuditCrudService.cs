using Skillforge.Domain;
using Skillforge.Dto;
using Skillforge.Repository;

namespace Skillforge.Service
{
	public class AuditCrudService:IAuditCrudService
	{
		private readonly IAuditCrudRepository _repository;

		public AuditCrudService(IAuditCrudRepository repository)
		{
			_repository = repository;
		}

		public async Task<List<AuditResponseDto>> GetAllAsync()
		{
			var audits = await _repository.GetAllAsync();
			return audits.Select(a => new AuditResponseDto
			{
				AuditID = a.AuditID,
				HRID = a.HRID,
				HRName = a.HRUser?.Name ?? string.Empty,
				Scope = a.Scope.ToString(),
				Findings = a.Findings,
				Date = a.Date,
				Status = a.Status
			}).ToList();
		}

		public async Task<AuditResponseDto> CreateAsync(CreateAuditDto dto, int hrId)
		{
			if (!Enum.TryParse<ReportScope>(dto.Scope, ignoreCase: true, out var scope))
				throw new ArgumentException($"Invalid scope '{dto.Scope}'. Must be Course, Employee, or Department.");

			if (string.IsNullOrWhiteSpace(dto.Findings))
				throw new ArgumentException("Findings cannot be empty.");

			var audit = new Audit
			{
				HRID = hrId,
				Scope = scope,
				Findings = dto.Findings,
				Date = DateTime.Now,
				Status = dto.Status
			};

			var created = await _repository.CreateAsync(audit);

			return new AuditResponseDto
			{
				AuditID = created.AuditID,
				HRID = created.HRID,
				HRName = string.Empty,
				Scope = created.Scope.ToString(),
				Findings = created.Findings,
				Date = created.Date,
				Status = created.Status
			};
		}

		public async Task<bool> DeleteAsync(int auditId)
		{
			return await _repository.DeleteAsync(auditId);
		}
	}
}
