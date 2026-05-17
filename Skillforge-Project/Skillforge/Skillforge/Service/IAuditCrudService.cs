using Skillforge.Dto;

namespace Skillforge.Service
{
	public interface IAuditCrudService
	{
		Task<List<AuditResponseDto>> GetAllAsync();
		Task<AuditResponseDto> CreateAsync(CreateAuditDto dto, int hrId);
		Task<bool> DeleteAsync(int auditId);
	}

}
