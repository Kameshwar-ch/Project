using Skillforge.Domain;

namespace Skillforge.Dto
{
	public class CompetencyMatrixSearchDto
	{
		public int? EmployeeId { get; set; }
		public CompetencyLevel? Level { get; set; }
		public string? SkillName { get; set; }
		public string? EmployeeName { get; set; }
	}
}
