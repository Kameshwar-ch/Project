using Skillforge.Domain;
using Skillforge.Dto;
using Skillforge.Repository;

namespace Skillforge.Service
{
	public class CompetencyService : ICompetencyService
	{
		private readonly ICompetencyRepository _repository;

		public CompetencyService(ICompetencyRepository repository)
		{
			_repository = repository;
		}

		public async Task<List<CompetencyMatrixDto>> GetCompetencyMatrixAsync(CompetencyMatrixSearchDto searchDto)
		{
			var employees = await _repository.GetEmployeesWithSkillsAsync();

			return employees
				.Where(u => (searchDto.EmployeeId == null || u.UserID == searchDto.EmployeeId) && (string.IsNullOrWhiteSpace(searchDto.EmployeeName) ||
							u.Name.Contains(searchDto.EmployeeName, StringComparison.OrdinalIgnoreCase)))
				.Select(u => new CompetencyMatrixDto
				{
					EmployeeId = u.UserID,
					EmployeeName = u.Name,
					Skills = u.SkillGaps
						.Where(sg =>
							(searchDto.Level == null || sg.Competency.Level == searchDto.Level) &&
							(string.IsNullOrWhiteSpace(searchDto.SkillName) ||
							 sg.Competency.Name.Contains(searchDto.SkillName, StringComparison.OrdinalIgnoreCase))
						)
						.Select(sg => new EmployeeSkillDto
						{
							SkillName = sg.Competency.Name,
							Level = sg.Competency.Level.ToString()
						}).ToList()
				})
				.Where(dto => dto.Skills.Any() ||
							 (string.IsNullOrWhiteSpace(searchDto.EmployeeName) &&
							  string.IsNullOrWhiteSpace(searchDto.SkillName) &&
							  searchDto.Level == null))
				.ToList();
		}

		public async Task<List<CompetencyResponseDto>> GetAllCompetenciesAsync()
		{
			var list = await _repository.GetAllAsync();
			return list.Select(c => new CompetencyResponseDto
			{
				CompetencyId = c.CompetencyID,
				Name         = c.Name,
				Description  = c.Description ?? string.Empty,
				Level        = c.Level.ToString()
			}).ToList();
		}

		public async Task<CompetencyResponseDto> CreateCompetencyAsync(CreateCompetencyDto dto)
		{
			if (string.IsNullOrWhiteSpace(dto.Name))
				throw new ArgumentException("Competency name is required.");

			var competency = new Competency
			{
				Name        = dto.Name.Trim(),
				Description = dto.Description?.Trim() ?? string.Empty,
				Level       = dto.Level
			};
			await _repository.AddAsync(competency);
			await _repository.SaveAsync();

			return new CompetencyResponseDto
			{
				CompetencyId = competency.CompetencyID,
				Name         = competency.Name,
				Description  = competency.Description,
				Level        = competency.Level.ToString()
			};
		}

		public async Task<bool> UpdateCompetencyAsync(int id, UpdateCompetencyDto dto)
		{
			if (string.IsNullOrWhiteSpace(dto.Name))
				throw new ArgumentException("Competency name is required.");

			bool updated = await _repository.UpdateAsync(id, dto.Name.Trim(), dto.Description?.Trim() ?? string.Empty, dto.Level);
			if (!updated) return false;
			await _repository.SaveAsync();
			return true;
		}

		public async Task<bool> DeleteCompetencyAsync(int id)
		{
			bool deleted = await _repository.DeleteAsync(id);
			if (!deleted) return false;
			await _repository.SaveAsync();
			return true;
		}
	}
}
