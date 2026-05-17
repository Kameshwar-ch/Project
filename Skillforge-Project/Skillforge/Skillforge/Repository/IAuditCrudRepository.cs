using Skillforge.Domain;

namespace Skillforge.Repository
{
	public interface IAuditCrudRepository
	{
		Task<List<Audit>> GetAllAsync();
		Task<Audit> CreateAsync(Audit audit);
		Task<bool> DeleteAsync(int auditId);
	}
}
