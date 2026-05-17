using Skillforge.Dto;
using System.Threading.Tasks;

namespace Skillforge.Service
{
	public interface ICourseService
	{
		Task<int> CreateModuleAsync(int courseId, CreateModuleDto dto, int? trainerId);
        Task CreateCourseAsync(CourseRequestDto courseRequest);
	    Task<CourseResponseDto> GetCourseByIDAsync(int courseID, int userID);
		Task<List<CourseResponseDto>> GetCoursesAsync(CourseFilterRequestDto request);
		Task<bool> UpdateCourseStatus(int courseId, bool status);
		Task<bool> UpdateCourseAsync(int courseId, UpdateCourseDto dto);
		Task<bool> DeleteCourseAsync(int courseId);
		Task<List<ModuleResponseDto>> GetModulesByCourseAsync(int courseId);
		Task<bool> UpdateModuleAsync(int courseId, int moduleId, UpdateModuleDto dto);
		Task<bool> DeleteModuleAsync(int courseId, int moduleId);
	}
}

