using Skillforge.Dto;

namespace Skillforge.Service
{
	public interface ICompetencyService
	{
		Task<List<CompetencyMatrixDto>> GetCompetencyMatrixAsync(CompetencyMatrixSearchDto searchDto);
		Task<List<CompetencyResponseDto>> GetAllCompetenciesAsync();
		Task<CompetencyResponseDto> CreateCompetencyAsync(CreateCompetencyDto dto);
		Task<bool> UpdateCompetencyAsync(int id, UpdateCompetencyDto dto);
		Task<bool> DeleteCompetencyAsync(int id);
	}
}
