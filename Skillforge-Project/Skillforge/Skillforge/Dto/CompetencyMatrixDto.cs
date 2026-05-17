namespace Skillforge.Dto
{
	public class CompetencyMatrixDto
	{
		public int EmployeeId { get; set; }
		public string EmployeeName { get; set; } = string.Empty;
		public List<EmployeeSkillDto> Skills { get; set; } = new();
	}
}
