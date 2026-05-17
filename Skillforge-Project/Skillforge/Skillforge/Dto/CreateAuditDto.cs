using System.ComponentModel.DataAnnotations;

namespace Skillforge.Dto
{
	public class CreateAuditDto
	{
		[Required]
		public string Scope { get; set; } = string.Empty;

		[Required]
		public string Findings { get; set; } = string.Empty;

		[Required]
		public bool Status { get; set; }
	}
}
