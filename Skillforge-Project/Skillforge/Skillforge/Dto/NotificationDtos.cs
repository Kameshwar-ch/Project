namespace Skillforge.Dto;

public class NotificationResponseDto
{
    public int NotificationId { get; set; }
    public int UserId { get; set; }
    public int? CourseId { get; set; }
    public string Message { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public DateTime CreatedDate { get; set; }
}
