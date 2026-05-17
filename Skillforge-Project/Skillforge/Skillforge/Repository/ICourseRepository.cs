using System.Threading.Tasks;
using Skillforge.Domain;
using Skillforge.Dto;
namespace Skillforge.Repository
{
    public interface ICourseRepository
    {

        Task<Course> CreateCourseAsync(Course course);
        
        Task<bool> TrainerExistsAsync(int trainerId);
        
        Task<int> SaveAsync();

        Task<Course?> GetCourseByIdAsync(int courseId);
        
		Task<int> AddModuleAsync(Module module);
        
        Task<Course?> GetByIDAsync(int courseID);

         Task<List<CourseResponseDto>> GetCoursesFilteredAsync(CourseFilterRequestDto request);

        Task<bool> UpdateCourseAsync(int courseId, UpdateCourseDto dto);
        Task<bool> DeleteCourseAsync(int courseId);

        Task<List<ModuleResponseDto>> GetModulesByCourseAsync(int courseId);
        Task<bool> UpdateModuleAsync(int courseId, int moduleId, UpdateModuleDto dto);
        Task<bool> DeleteModuleAsync(int courseId, int moduleId);

		Task<bool> CourseTitleExistsAsync(string title);
	}
}
