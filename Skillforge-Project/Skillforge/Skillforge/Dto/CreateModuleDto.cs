namespace Skillforge.Dto
{
	public class CreateModuleDto
	{
		public string Title { get; set; } = string.Empty;
		public string ContentURI { get; set; } = string.Empty;
		public decimal Duration { get; set; }
	}

	public class UpdateModuleDto
	{
		public string Title { get; set; } = string.Empty;
		public string ContentURI { get; set; } = string.Empty;
		public decimal Duration { get; set; }
	}
}
