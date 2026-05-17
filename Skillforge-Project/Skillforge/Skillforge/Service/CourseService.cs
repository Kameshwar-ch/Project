using Microsoft.EntityFrameworkCore;
using Skillforge.Domain;
using Skillforge.Data;
using Skillforge.Dto;
using Skillforge.Repository;
using Skillforge.Utility;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Skillforge.Service
{
    public class CourseService : ICourseService
    {
        private readonly ICourseRepository _courseRepository;
        private readonly IAuditService     _auditService;
        private readonly SkillForgeDB      _context;

        private const string CourseAccessedAction = "CourseAccessed";

        public CourseService(ICourseRepository courseRepository, IAuditService auditService, SkillForgeDB context)
        {
            _courseRepository = courseRepository;
            _auditService     = auditService;
            _context          = context;
        }

        public async Task<int> CreateModuleAsync(int courseId, CreateModuleDto dto, int? trainerId)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(dto.Title))
					throw new ArgumentException(CourseMessages.InvalidTitle);

				if (string.IsNullOrWhiteSpace(dto.ContentURI))
					throw new ArgumentException(CourseMessages.InvalidURI);

                if (dto.Duration <= 0)
                    throw new ArgumentException(CourseMessages.InvalidDuration);

                var course = await _courseRepository.GetCourseByIdAsync(courseId);

                if (course == null)
                    throw new KeyNotFoundException(CourseMessages.CourseNotFound);

                if (course.Status != false)
                    throw new InvalidOperationException(CourseMessages.NotInDraft);

                var newModule = new Module
                {
                    CourseID   = courseId,
                    Title      = dto.Title,
                    ContentURI = dto.ContentURI,
                    Duration   = dto.Duration,
                    Status     = false
                };

                int moduleId = await _courseRepository.AddModuleAsync(newModule);
                await _auditService.LogAsync(trainerId, "Module Created Successfully", $"Module: {dto.Title} (ID: {moduleId}) for Course: {courseId}");
                return moduleId;
            }
            catch (Exception ex)
            {
                await _auditService.LogAsync(trainerId, "Module Creation Failed", $"CourseID: {courseId}, Error: {ex.Message}");
                throw;
            }
        }

       public async Task CreateCourseAsync(CourseRequestDto courseRequest)
        {
            bool trainerExists = await _courseRepository.TrainerExistsAsync(courseRequest.TrainerID);
            
            if (!trainerExists)
            {
                throw new KeyNotFoundException($"Trainer with ID {courseRequest.TrainerID} not found.");
            }
			bool titleExists = await _courseRepository.CourseTitleExistsAsync(courseRequest.Title);
			if (titleExists)
			{
				throw new InvalidOperationException($"A course with the title '{courseRequest.Title}' already exists.");
			}
			var newCourse = new Course
            {
                Title = courseRequest.Title,
                Description = courseRequest.Description,
                TrainerID = courseRequest.TrainerID,
                Duration = courseRequest.Duration,
                Status = false 
            };
            await _courseRepository.CreateCourseAsync(newCourse);
            await _courseRepository.SaveAsync();

            try
            {
                await _auditService.LogAsync(newCourse.TrainerID, "CourseCreated", $"New course '{newCourse.Title}' created with status 0.");
            }
            catch { /* audit log failure must not abort a successful course creation */ }
        }

        public async Task<CourseResponseDto> GetCourseByIDAsync(int courseID, int userID)
        {
            if (courseID <= 0)
                throw new InvalidOperationException("Invalid CourseID.");

            var course = await _context.Courses
                .Include(c => c.UserIDNavigation)
                .FirstOrDefaultAsync(c => c.CourseID == courseID);

            if (course == null)
                throw new KeyNotFoundException($"Course {courseID} not found.");

            // Any enrollment (active or completed) allows viewing
            var isEnrolled = await _context.Enrollments
                .AnyAsync(e =>
                    e.EmployeeID == userID  &&
                    e.CourseID   == courseID);

            if (!isEnrolled)
                throw new UnauthorizedAccessException("You are not enrolled in this course.");

			// Only log for active enrollments (Status == false means Active)
			// Completed employees can view but NOT logged
			// Only log for enrolled (active) employees — completed ones can view but aren't logged
			var isActive = await _context.Enrollments
				.AnyAsync(e =>
					e.EmployeeID == userID &&
					e.CourseID == courseID &&
					e.Status == true &&
					!e.CompletedDate.HasValue);

			if (isActive)
            {
                _context.AuditLogs.Add(new AuditLog
                {
                    UserID    = userID,
                    Action    = CourseAccessedAction,
                    Resource  = $"Course/{courseID}",
                    Timestamp = DateTime.Now
                });
                await _context.SaveChangesAsync();
            }

            var enrollmentCount = await _context.Enrollments
                .CountAsync(e => e.CourseID == courseID);

            return new CourseResponseDto
            {
                CourseID        = course.CourseID,
                Title           = course.Title,
                Description     = course.Description,
                TrainerID       = course.TrainerID,
                TrainerName     = course.UserIDNavigation?.Name ?? string.Empty,
                Duration        = course.Duration,
                Status          = course.Status,
                EnrollmentCount = enrollmentCount
            };
        }
		public async Task<bool> UpdateCourseAsync(int courseId, UpdateCourseDto dto)
		{
			if (!dto.Status)
			{
				bool hasEnrollments = await _context.Enrollments.AnyAsync(e => e.CourseID == courseId);
				if (hasEnrollments)
					throw new InvalidOperationException("Cannot set course to draft. Employees are already enrolled.");
			}

			bool titleExists = await _context.Courses
				.AnyAsync(c => c.Title.ToLower() == dto.Title.ToLower() && c.CourseID != courseId);
			if (titleExists)
			{
				throw new InvalidOperationException($"A course with the title '{dto.Title}' already exists.");
			}

			return await _courseRepository.UpdateCourseAsync(courseId, dto);
		}

		public async Task<bool> UpdateCourseStatus(int courseId, bool status)
		{
			var course = await _courseRepository.GetCourseByIdAsync(courseId);
			if (course == null) return false;
			course.Status = status;
			await _courseRepository.SaveAsync();
			return true;
		}

		public async Task<List<CourseResponseDto>> GetCoursesAsync(CourseFilterRequestDto request)
		{
			return await _courseRepository.GetCoursesFilteredAsync(request);
		}

		public async Task<bool> DeleteCourseAsync(int courseId)
		{
			return await _courseRepository.DeleteCourseAsync(courseId);
		}

		public async Task<List<ModuleResponseDto>> GetModulesByCourseAsync(int courseId)
		{
			return await _courseRepository.GetModulesByCourseAsync(courseId);
		}

		public async Task<bool> UpdateModuleAsync(int courseId, int moduleId, UpdateModuleDto dto)
		{
			if (string.IsNullOrWhiteSpace(dto.Title))
				throw new ArgumentException(CourseMessages.InvalidTitle);

			if (string.IsNullOrWhiteSpace(dto.ContentURI))
				throw new ArgumentException(CourseMessages.InvalidURI);

			if (dto.Duration <= 0)
				throw new ArgumentException(CourseMessages.InvalidDuration);

			var course = await _courseRepository.GetCourseByIdAsync(courseId);
			if (course == null)
				throw new KeyNotFoundException(CourseMessages.CourseNotFound);

			return await _courseRepository.UpdateModuleAsync(courseId, moduleId, dto);
		}

		public async Task<bool> DeleteModuleAsync(int courseId, int moduleId)
		{
			var course = await _courseRepository.GetCourseByIdAsync(courseId);
			if (course == null)
				throw new KeyNotFoundException(CourseMessages.CourseNotFound);

			return await _courseRepository.DeleteModuleAsync(courseId, moduleId);
		}
    }
}