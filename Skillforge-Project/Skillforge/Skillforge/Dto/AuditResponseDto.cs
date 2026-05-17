namespace Skillforge.Dto
{
	public class AuditResponseDto
	{
		public int AuditID { get; set; }
		public int HRID { get; set; }
		public string HRName { get; set; } = string.Empty;
		public string Scope { get; set; } = string.Empty;
		public string Findings { get; set; } = string.Empty;
		public DateTime Date { get; set; }
		public bool Status { get; set; }
	}
}
