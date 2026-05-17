using Skillforge.Domain;

namespace Skillforge.Repository
{
	public interface ICompetencyRepository
	{
		Task<List<User>> GetEmployeesWithSkillsAsync();
		Task<List<Competency>> GetAllAsync();
		Task<Competency?> GetByIdAsync(int id);
		Task AddAsync(Competency competency);
		Task<bool> UpdateAsync(int id, string name, string description, CompetencyLevel level);
		Task<bool> DeleteAsync(int id);
		Task SaveAsync();
	}
}
