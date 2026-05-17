using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Skillforge.Data;
using Skillforge.Domain;
using Skillforge.Dto;
using Swashbuckle.Swagger;

namespace Skillforge.Repository
{
    public class CourseRepository : ICourseRepository
    {
        private readonly SkillForgeDB _context;

        public CourseRepository(SkillForgeDB context)
        {
            _context = context;
        }

        public async Task<Course> CreateCourseAsync(Course course)
        {
            await _context.Courses.AddAsync(course);
            return course;
        }

        public async Task<bool> TrainerExistsAsync(int trainerId)
        {
            return await _context.Users.AnyAsync(u => u.UserID == trainerId);
        }

        public async Task<int> SaveAsync()
        {
            return await _context.SaveChangesAsync();
        }

        public async Task<Course?> GetCourseByIdAsync(int courseId)
        {
            return await _context.Courses.FindAsync(courseId);
        }

        public async Task<int> AddModuleAsync(Module module)
        {
            _context.Modules.Add(module);
            await _context.SaveChangesAsync();
            return module.ModuleID;
        }

        public async Task<Course?> GetByIDAsync(int courseID)
        {
            return await _context.Courses
                .FirstOrDefaultAsync(c => c.CourseID == courseID);
        }

        public async Task<bool> UpdateCourseAsync(int courseId, UpdateCourseDto dto)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null) return false;
            course.Title       = dto.Title;
            course.Description = dto.Description;
            course.TrainerID   = dto.TrainerID;
            course.Duration    = dto.Duration;
            course.Status      = dto.Status;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<ModuleResponseDto>> GetModulesByCourseAsync(int courseId)
        {
            return await _context.Modules
                .Where(m => m.CourseID == courseId)
                .Select(m => new ModuleResponseDto
                {
                    ModuleID    = m.ModuleID,
                    CourseID    = m.CourseID,
                    Title       = m.Title,
                    ContentURI  = m.ContentURI,
                    Duration    = m.Duration,
                    Status      = m.Status
                })
                .ToListAsync();
        }

        public async Task<bool> UpdateModuleAsync(int courseId, int moduleId, UpdateModuleDto dto)
        {
            var module = await _context.Modules
                .FirstOrDefaultAsync(m => m.ModuleID == moduleId && m.CourseID == courseId);
            if (module == null) return false;

            module.Title      = dto.Title;
            module.ContentURI = dto.ContentURI;
            module.Duration   = dto.Duration;
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteModuleAsync(int courseId, int moduleId)
        {
            var module = await _context.Modules
                .FirstOrDefaultAsync(m => m.ModuleID == moduleId && m.CourseID == courseId);
            if (module == null) return false;

            _context.Modules.Remove(module);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<bool> DeleteCourseAsync(int courseId)
        {
            var course = await _context.Courses.FindAsync(courseId);
            if (course == null) return false;
            _context.Courses.Remove(course);
            await _context.SaveChangesAsync();
            return true;
        }

        public async Task<List<CourseResponseDto>> GetCoursesFilteredAsync(CourseFilterRequestDto request)
        {
            IQueryable<Course> query = _context.Courses.AsNoTracking();

            if (request.Status.HasValue)
                query = query.Where(c => c.Status == request.Status.Value);

            if (request.TrainerId.HasValue)
                query = query.Where(c => c.TrainerID == request.TrainerId.Value);

            if (request.MinDuration.HasValue)
                query = query.Where(c => c.Duration >= request.MinDuration.Value);

            if (request.MaxDuration.HasValue)
                query = query.Where(c => c.Duration <= request.MaxDuration.Value);

            return await query
                .Join(_context.Users,
                      c => c.TrainerID,
                      u => u.UserID,
                      (c, u) => new { c, u })
                .GroupJoin(_context.Enrollments,
                      cu => cu.c.CourseID,
                      e  => e.CourseID,
                      (cu, enrollments) => new CourseResponseDto
                      {
                          CourseID        = cu.c.CourseID,
                          Title           = cu.c.Title,
                          Description     = cu.c.Description,
                          TrainerID       = cu.c.TrainerID,
                          TrainerName     = cu.u.Name,
                          Duration        = cu.c.Duration,
                          Status          = cu.c.Status,
                          EnrollmentCount = enrollments.Count()
                      })
                .ToListAsync();
        }
		public async Task<bool> CourseTitleExistsAsync(string title)
		{
			return await _context.Courses.AnyAsync(c => c.Title.ToLower() == title.ToLower());
		}
	}
   
	}