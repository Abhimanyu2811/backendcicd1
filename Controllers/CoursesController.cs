using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backendapi.Data;
using Backendapi.Models;
using finalpracticeproject.DTOs;
using Microsoft.Extensions.Logging;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using System.Text.Json;
using Backendapi.Services;

namespace Backendapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class CoursesController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<CoursesController> _logger;
        private readonly ITelemetryService _telemetry;

        public CoursesController(AppDbContext context, ILogger<CoursesController> logger, ITelemetryService telemetry)
        {
            _context = context;
            _logger = logger;
            _telemetry = telemetry;
        }

        // GET: api/Courses
        [HttpGet]
        public async Task<ActionResult<IEnumerable<object>>> GetCourses()
        {
            _telemetry.TrackEvent("GetAllCourses");
            try
            {
                _logger.LogInformation("Fetching all courses...");

                if (_context.Courses == null)
                {
                    _logger.LogError("Courses DbSet is null");
                    return BadRequest(new { message = "Database context is not properly initialized" });
                }

                var courses = await _context.Courses
                    .Include(c => c.Instructor)
                    .ToListAsync();

                _logger.LogInformation($"Found {courses.Count} courses");

                var response = courses.Select(c => new
                {
                    courseId = c.CourseId,
                    title = c.Title ?? "Untitled Course",
                    description = c.Description ?? "No description available",
                    mediaUrl = c.MediaUrl,
                    instructor = c.Instructor != null ? new
                    {
                        userId = c.Instructor.UserId,
                        name = c.Instructor.Name ?? "Unknown Instructor",
                        email = c.Instructor.Email
                    } : null
                }).ToList();

                _telemetry.TrackMetric("CoursesRetrieved", courses.Count);
                return Ok(response);
            }
            catch (Exception ex)
            {
                _telemetry.TrackException(ex);
                _logger.LogError(ex, "Error in GetCourses");
                return StatusCode(500, new
                {
                    message = "Internal server error while fetching courses",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        // GET: api/Courses/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Course>> GetCourse(Guid id)
        {
            _telemetry.TrackEvent("GetCourseById", new Dictionary<string, string> { { "CourseId", id.ToString() } });
            try
            {
                var course = await _context.Courses.FindAsync(id);

                if (course == null)
                {
                    _telemetry.TrackEvent("CourseNotFound", new Dictionary<string, string> { { "CourseId", id.ToString() } });
                    return NotFound();
                }

                return course;
            }
            catch (Exception ex)
            {
                _telemetry.TrackException(ex);
                throw;
            }
        }

        // PUT: api/Courses/5
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPut("{id}")]
        public async Task<IActionResult> PutCourse(Guid id, CourseCreateDto courseDto)
        {
            _telemetry.TrackEvent("UpdateCourse", new Dictionary<string, string> { { "CourseId", id.ToString() } });
            try
            {
                if (id != courseDto.CourseId)
                {
                    _telemetry.TrackEvent("CourseIdMismatch", new Dictionary<string, string>
                    {
                        { "RequestedId", id.ToString() },
                        { "DtoId", courseDto.CourseId.ToString() }
                    });
                    return BadRequest("ID mismatch");
                }

                var course = await _context.Courses.FindAsync(id);
                if (course == null)
                {
                    _telemetry.TrackEvent("CourseNotFoundForUpdate", new Dictionary<string, string> { { "CourseId", id.ToString() } });
                    return NotFound();
                }

                course.Title = courseDto.Title;
                course.Description = courseDto.Description;
                course.InstructorId = courseDto.InstructorId;
                course.MediaUrl = courseDto.MediaUrl;
                course.CourseUrl = courseDto.CourseUrl;

                await _context.SaveChangesAsync();
                _telemetry.TrackEvent("CourseUpdated", new Dictionary<string, string> { { "CourseId", id.ToString() } });

                return NoContent();
            }
            catch (Exception ex)
            {
                _telemetry.TrackException(ex);
                throw;
            }
        }


        // POST: api/Courses
        // To protect from overposting attacks, see https://go.microsoft.com/fwlink/?linkid=2123754
        [HttpPost]
        public async Task<ActionResult<Course>> PostCourse(CourseCreateDto courseDto)
        {
            _telemetry.TrackEvent("CreateCourse", new Dictionary<string, string> { { "Title", courseDto.Title } });
            try
            {
                _logger.LogInformation($"Creating new course with data: {JsonSerializer.Serialize(courseDto)}");

                if (courseDto.InstructorId == null || courseDto.InstructorId == Guid.Empty)
                {
                    _logger.LogWarning("InstructorId is null or empty");
                    return BadRequest(new { message = "InstructorId is required" });
                }

                // Validate and process CourseUrl
                string processedCourseUrl = null;
                if (!string.IsNullOrEmpty(courseDto.CourseUrl))
                {
                    // If it's a YouTube URL, convert it to embed format
                    if (courseDto.CourseUrl.Contains("youtube.com/watch?v="))
                    {
                        var videoId = courseDto.CourseUrl.Split("v=")[1];
                        processedCourseUrl = $"https://www.youtube.com/embed/{videoId}";
                    }
                    else
                    {
                        processedCourseUrl = courseDto.CourseUrl;
                    }
                }

                var course = new Course
                {
                    CourseId = courseDto.CourseId,
                    Title = courseDto.Title,
                    Description = courseDto.Description,
                    InstructorId = courseDto.InstructorId,
                    MediaUrl = courseDto.MediaUrl,
                    CourseUrl = processedCourseUrl
                };

                _logger.LogInformation($"Created course object: {JsonSerializer.Serialize(course)}");

                _context.Courses.Add(course);
                await _context.SaveChangesAsync();

                _telemetry.TrackEvent("CourseCreated", new Dictionary<string, string>
                {
                    { "CourseId", course.CourseId.ToString() },
                    { "Title", course.Title },
                    { "InstructorId", course.InstructorId.ToString() }
                });

                _logger.LogInformation($"Successfully created course with ID: {course.CourseId}");

                // Return the created course with a 201 status code
                return CreatedAtAction(nameof(GetCourse), new { id = course.CourseId }, new
                {
                    courseId = course.CourseId,
                    title = course.Title,
                    description = course.Description,
                    instructorId = course.InstructorId,
                    mediaUrl = course.MediaUrl,
                    courseUrl = course.CourseUrl
                });
            }
            catch (Exception ex)
            {
                _telemetry.TrackException(ex);
                _logger.LogError(ex, "Error creating course");
                return StatusCode(500, new
                {
                    message = "Error creating course",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }


        // DELETE: api/Courses/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteCourse(Guid id)
        {
            _telemetry.TrackEvent("DeleteCourse", new Dictionary<string, string> { { "CourseId", id.ToString() } });
            try
            {
                var course = await _context.Courses.FindAsync(id);
                if (course == null)
                {
                    _telemetry.TrackEvent("CourseNotFoundForDeletion", new Dictionary<string, string> { { "CourseId", id.ToString() } });
                    return NotFound();
                }

                _context.Courses.Remove(course);
                await _context.SaveChangesAsync();
                _telemetry.TrackEvent("CourseDeleted", new Dictionary<string, string> { { "CourseId", id.ToString() } });

                return NoContent();
            }
            catch (Exception ex)
            {
                _telemetry.TrackException(ex);
                throw;
            }
        }

        // GET: api/Courses/available
        [HttpGet("available")]
        public async Task<ActionResult<IEnumerable<object>>> GetAvailableCourses()
        {
            try
            {
                _logger.LogInformation("Starting GetAvailableCourses...");

                if (_context.Courses == null)
                {
                    _logger.LogError("Courses DbSet is null");
                    return BadRequest(new { message = "Database context is not properly initialized" });
                }

                // Test database connection
                try
                {
                    _logger.LogInformation("Testing database connection...");
                    var canConnect = await _context.Database.CanConnectAsync();
                    if (!canConnect)
                    {
                        _logger.LogError("Cannot connect to database");
                        return BadRequest(new { message = "Cannot connect to database" });
                    }
                    _logger.LogInformation("Database connection successful");
                }
                catch (Exception dbEx)
                {
                    _logger.LogError(dbEx, "Database connection error");
                    return BadRequest(new
                    {
                        message = "Database connection error",
                        error = dbEx.Message,
                        stackTrace = dbEx.StackTrace
                    });
                }

                // Get courses with instructor
                _logger.LogInformation("Fetching courses with instructor data...");
                var courses = await _context.Courses
                    .Include(c => c.Instructor)
                    .ToListAsync();

                _logger.LogInformation($"Found {courses.Count} courses");

                if (courses.Count == 0)
                {
                    _logger.LogInformation("No courses found, returning empty list");
                    return Ok(new List<object>());
                }

                // Map to response format
                var response = courses.Select(c => new
                {
                    courseId = c.CourseId,
                    title = c.Title ?? "Untitled Course",
                    description = c.Description ?? "No description available",
                    mediaUrl = c.MediaUrl,
                    instructor = c.Instructor != null ? new
                    {
                        userId = c.Instructor.UserId,
                        name = c.Instructor.Name ?? "Unknown Instructor",
                        email = c.Instructor.Email
                    } : new
                    {
                        userId = Guid.Empty,
                        name = "No Instructor Assigned",
                        email = "N/A"
                    }
                }).ToList();

                _logger.LogInformation("Successfully mapped courses to response format");
                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetAvailableCourses");
                return StatusCode(500, new
                {
                    message = "Internal server error while fetching courses",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        /// <summary>
        /// Gets all courses for the currently authenticated instructor
        /// </summary>
        /// <returns>A list of courses created by the instructor</returns>
        /// <response code="200">Returns the list of courses</response>
        /// <response code="401">If the user is not authenticated</response>
        /// <response code="404">If no courses are found</response>
        [HttpGet("instructor")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<ActionResult<IEnumerable<object>>> GetInstructorCourses()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    _logger.LogWarning("User ID not found in token");
                    return Unauthorized(new { message = "User ID not found in token" });
                }

                if (!Guid.TryParse(userIdClaim.Value, out Guid userId))
                {
                    _logger.LogWarning("Invalid user ID format in token");
                    return BadRequest(new { message = "Invalid user ID format" });
                }

                _logger.LogInformation($"User ID from token: {userId}");

                var courses = await _context.Courses
                    .Include(c => c.Instructor)
                    .Where(c => c.InstructorId == userId)
                    .ToListAsync();

                if (courses == null || !courses.Any())
                {
                    _logger.LogInformation($"No courses found for instructor {userId}");
                    return NotFound(new { message = "No courses found for this instructor" });
                }

                var response = courses.Select(c => new
                {
                    courseId = c.CourseId,
                    title = c.Title ?? "Untitled Course",
                    description = c.Description ?? "No description available",
                    mediaUrl = c.MediaUrl,
                    instructor = c.Instructor != null ? new
                    {
                        userId = c.Instructor.UserId,
                        name = c.Instructor.Name ?? "Unknown Instructor",
                        email = c.Instructor.Email
                    } : null
                }).ToList();

                return Ok(response);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetInstructorCourses");
                return StatusCode(500, new
                {
                    message = "Internal server error while fetching instructor courses",
                    error = ex.Message
                });
            }
        }

        [Authorize]
        [HttpGet("enrolled")]
        public async Task<ActionResult<IEnumerable<object>>> GetEnrolledCourses()
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    _logger.LogWarning("User ID not found in token");
                    return Unauthorized(new { message = "User ID not found in token" });
                }

                if (!Guid.TryParse(userIdClaim.Value, out Guid userId))
                {
                    _logger.LogWarning("Invalid user ID format in token");
                    return BadRequest(new { message = "Invalid user ID format" });
                }

                _logger.LogInformation($"User ID from token: {userId}");

                var user = await _context.Users
                    .Include(u => u.EnrolledCourses)
                        .ThenInclude(c => c.Instructor)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null)
                {
                    _logger.LogWarning($"User not found: {userId}");
                    return NotFound(new { message = "User not found" });
                }

                var enrolledCourses = user.EnrolledCourses.Select(c => new
                {
                    courseId = c.CourseId,
                    title = c.Title ?? "Untitled Course",
                    description = c.Description ?? "No description available",
                    mediaUrl = c.MediaUrl,
                    instructor = c.Instructor != null ? new
                    {
                        userId = c.Instructor.UserId,
                        name = c.Instructor.Name ?? "Unknown Instructor",
                        email = c.Instructor.Email
                    } : null
                }).ToList();

                _logger.LogInformation($"Found {enrolledCourses.Count} enrolled courses for user {userId}");
                return Ok(enrolledCourses);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in GetEnrolledCourses");
                return StatusCode(500, new
                {
                    message = "Internal server error while fetching enrolled courses",
                    error = ex.Message
                });
            }
        }

        // POST: api/Courses/{courseId}/enroll
        [HttpPost("{courseId}/enroll")]
        public async Task<IActionResult> Enroll(Guid courseId)
        {
            try
            {
                var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
                if (userIdClaim == null)
                {
                    _logger.LogWarning("User ID not found in token");
                    return Unauthorized(new { message = "User ID not found in token" });
                }

                if (!Guid.TryParse(userIdClaim.Value, out Guid userId))
                {
                    _logger.LogWarning("Invalid user ID format in token");
                    return BadRequest(new { message = "Invalid user ID format" });
                }

                _logger.LogInformation($"User ID from token: {userId}");

                var user = await _context.Users
                    .Include(u => u.EnrolledCourses)
                    .FirstOrDefaultAsync(u => u.UserId == userId);

                if (user == null)
                {
                    _logger.LogWarning($"User not found: {userId}");
                    return NotFound(new { message = "User not found" });
                }

                var course = await _context.Courses.FindAsync(courseId);
                if (course == null)
                {
                    _logger.LogWarning($"Course not found: {courseId}");
                    return NotFound(new { message = "Course not found" });
                }

                if (user.EnrolledCourses.Any(c => c.CourseId == courseId))
                {
                    _logger.LogWarning($"User {userId} is already enrolled in course {courseId}");
                    return BadRequest(new { message = "User is already enrolled in this course" });
                }

                user.EnrolledCourses.Add(course);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"User {userId} successfully enrolled in course {courseId}");
                return Ok(new { message = "Successfully enrolled in course" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in Enroll");
                return StatusCode(500, new
                {
                    message = "Internal server error while enrolling in course",
                    error = ex.Message
                });
            }
        }

        private bool CourseExists(Guid id)
        {
            return _context.Courses.Any(e => e.CourseId == id);
        }
    }
}
