using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using finalpracticeproject.DTOs;
using Backendapi.Data;
using Backendapi.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.Extensions.Logging;
using Backendapi.Services;

namespace Backendapi.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AssessmentsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<AssessmentsController> _logger;
        private readonly ITelemetryService _telemetry;

        public AssessmentsController(AppDbContext context, ILogger<AssessmentsController> logger, ITelemetryService telemetry)
        {
            _context = context;
            _logger = logger;
            _telemetry = telemetry;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Assessment>>> GetAssessments()
        {
            _telemetry.TrackEvent("GetAllAssessments");
            try
            {
                var assessments = await _context.Assessments
                    .Include(a => a.Questions)
                        .ThenInclude(q => q.Options)
                    .ToListAsync();

                _telemetry.TrackMetric("AssessmentsRetrieved", assessments.Count);
                return assessments;
            }
            catch (Exception ex)
            {
                _telemetry.TrackException(ex);
                throw;
            }
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Assessment>> GetAssessment(Guid id)
        {
            _telemetry.TrackEvent("GetAssessmentById", new Dictionary<string, string> { { "AssessmentId", id.ToString() } });
            try
            {
                var assessment = await _context.Assessments
                    .Include(a => a.Questions)
                        .ThenInclude(q => q.Options)
                    .FirstOrDefaultAsync(a => a.AssessmentId == id);

                if (assessment == null)
                {
                    _telemetry.TrackEvent("AssessmentNotFound", new Dictionary<string, string> { { "AssessmentId", id.ToString() } });
                    return NotFound();
                }

                return assessment;
            }
            catch (Exception ex)
            {
                _telemetry.TrackException(ex);
                throw;
            }
        }

        [HttpPut("{id}")]
        public async Task<IActionResult> PutAssessment(Guid id, AssessmentCreateDto assessmentDto)
        {
            _telemetry.TrackEvent("UpdateAssessment", new Dictionary<string, string> { { "AssessmentId", id.ToString() } });
            if (id != assessmentDto.AssessmentId)
            {
                _telemetry.TrackEvent("AssessmentIdMismatch", new Dictionary<string, string>
                {
                    { "RequestedId", id.ToString() },
                    { "DtoId", assessmentDto.AssessmentId.ToString() }
                });
                return BadRequest();
            }

            var assessment = await _context.Assessments
                .Include(a => a.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(a => a.AssessmentId == id);

            if (assessment == null)
            {
                _telemetry.TrackEvent("AssessmentNotFoundForUpdate", new Dictionary<string, string> { { "AssessmentId", id.ToString() } });
                return NotFound();
            }

            assessment.Title = assessmentDto.Title;
            assessment.MaxScore = assessmentDto.MaxScore;
            assessment.CourseId = assessmentDto.CourseId;

            _context.Options.RemoveRange(assessment.Questions.SelectMany(q => q.Options));
            _context.Questions.RemoveRange(assessment.Questions);

            foreach (var questionDto in assessmentDto.Questions)
            {
                var question = new Question
                {
                    QuestionId = questionDto.QuestionId,
                    AssessmentId = assessment.AssessmentId,
                    QuestionText = questionDto.QuestionText
                };

                foreach (var optionDto in questionDto.Options)
                {
                    question.Options.Add(new Option
                    {
                        OptionId = optionDto.OptionId,
                        Text = optionDto.Text,
                        IsCorrect = optionDto.IsCorrect
                    });
                }

                assessment.Questions.Add(question);
            }

            try
            {
                await _context.SaveChangesAsync();
                _telemetry.TrackEvent("AssessmentUpdated", new Dictionary<string, string> { { "AssessmentId", id.ToString() } });
            }
            catch (DbUpdateConcurrencyException ex)
            {
                _telemetry.TrackException(ex);
                if (!AssessmentExists(id))
                {
                    return NotFound();
                }
                else
                {
                    throw;
                }
            }

            return NoContent();
        }

        [HttpPost]
        public async Task<ActionResult<Assessment>> PostAssessment(AssessmentCreateDto assessmentDto)
        {
            _telemetry.TrackEvent("CreateAssessment", new Dictionary<string, string> { { "Title", assessmentDto.Title } });
            try
            {
                var assessment = new Assessment
                {
                    AssessmentId = assessmentDto.AssessmentId,
                    CourseId = assessmentDto.CourseId,
                    Title = assessmentDto.Title,
                    MaxScore = assessmentDto.MaxScore
                };

                foreach (var questionDto in assessmentDto.Questions)
                {
                    var question = new Question
                    {
                        QuestionId = questionDto.QuestionId,
                        AssessmentId = assessment.AssessmentId,
                        QuestionText = questionDto.QuestionText
                    };

                    foreach (var optionDto in questionDto.Options)
                    {
                        question.Options.Add(new Option
                        {
                            OptionId = optionDto.OptionId,
                            Text = optionDto.Text,
                            IsCorrect = optionDto.IsCorrect
                        });
                    }

                    assessment.Questions.Add(question);
                }

                _context.Assessments.Add(assessment);
                await _context.SaveChangesAsync();

                _telemetry.TrackEvent("AssessmentCreated", new Dictionary<string, string>
                {
                    { "AssessmentId", assessment.AssessmentId.ToString() },
                    { "Title", assessment.Title },
                    { "CourseId", assessment.CourseId.ToString() }
                });

                return CreatedAtAction("GetAssessment", new { id = assessment.AssessmentId }, new
                {
                    assessment.AssessmentId,
                    assessment.CourseId,
                    assessment.Title,
                    assessment.MaxScore,
                    Questions = assessment.Questions.Select(q => new
                    {
                        q.QuestionId,
                        q.QuestionText,
                        Options = q.Options.Select(o => new
                        {
                            o.OptionId,
                            o.Text,
                            o.IsCorrect
                        })
                    })
                });
            }
            catch (Exception ex)
            {
                _telemetry.TrackException(ex);
                throw;
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteAssessment(Guid id)
        {
            _telemetry.TrackEvent("DeleteAssessment", new Dictionary<string, string> { { "AssessmentId", id.ToString() } });
            try
            {
                _logger.LogInformation($"Attempting to delete assessment with ID: {id}");

                var assessment = await _context.Assessments
                    .Include(a => a.Results)
                    .Include(a => a.Questions)
                    .FirstOrDefaultAsync(a => a.AssessmentId == id);

                if (assessment == null)
                {
                    _logger.LogWarning($"Assessment with ID {id} not found");
                    _telemetry.TrackEvent("AssessmentNotFoundForDeletion", new Dictionary<string, string> { { "AssessmentId", id.ToString() } });
                    return NotFound(new { message = "Assessment not found" });
                }

                _logger.LogInformation($"Found {assessment.Results.Count} results and {assessment.Questions.Count} questions to delete");

                _context.Assessments.Remove(assessment);
                await _context.SaveChangesAsync();

                _logger.LogInformation($"Successfully deleted assessment with ID: {id}");
                _telemetry.TrackEvent("AssessmentDeleted", new Dictionary<string, string> { { "AssessmentId", id.ToString() } });
                return NoContent();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error deleting assessment with ID: {id}");
                _telemetry.TrackException(ex);
                return StatusCode(500, new
                {
                    message = "Error deleting assessment",
                    error = ex.Message,
                    innerError = ex.InnerException?.Message
                });
            }
        }

        private bool AssessmentExists(Guid id)
        {
            return _context.Assessments.Any(e => e.AssessmentId == id);
        }

        [HttpGet("course/{courseId}")]
        public async Task<ActionResult<IEnumerable<object>>> GetAssessmentsByCourse(Guid courseId)
        {
            try
            {
                _logger.LogInformation($"Fetching assessments for course {courseId}");

                var courseExists = await _context.Courses.AnyAsync(c => c.CourseId == courseId);
                if (!courseExists)
                {
                    _logger.LogWarning($"Course {courseId} not found");
                    return NotFound(new { message = "Course not found" });
                }

                var assessments = await _context.Assessments
                    .Include(a => a.Questions)
                    .Where(a => a.CourseId == courseId)
                    .Select(a => new
                    {
                        a.AssessmentId,
                        a.Title,
                        a.MaxScore,
                        QuestionCount = a.Questions.Count
                    })
                    .ToListAsync();

                _logger.LogInformation($"Found {assessments.Count} assessments for course {courseId}");
                return Ok(assessments);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, $"Error fetching assessments for course {courseId}");
                return StatusCode(500, new
                {
                    message = "An error occurred while fetching assessments",
                    details = ex.Message,
                    stackTrace = ex.StackTrace
                });
            }
        }

        [HttpGet("{assessmentId}/result/{studentId}")]
        public async Task<ActionResult<object>> GetAssessmentResult(Guid assessmentId, Guid studentId)
        {
            // First get the assessment with questions and options
            var assessment = await _context.Assessments
                .Include(a => a.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(a => a.AssessmentId == assessmentId);

            if (assessment == null)
            {
                return NotFound(new { message = "Assessment not found" });
            }

            // Get the result for this assessment and student
            var result = await _context.Results
                .Include(r => r.StudentAnswers)
                .FirstOrDefaultAsync(r => r.AssessmentId == assessmentId && r.UserId == studentId);

            if (result == null)
            {
                return NotFound(new { message = "No result found for this student and assessment" });
            }

            var answers = assessment.Questions.Select(q =>
            {
                var studentAnswer = result.StudentAnswers.FirstOrDefault(sa => sa.QuestionId == q.QuestionId);
                var selectedOption = q.Options.FirstOrDefault(o => o.OptionId == studentAnswer?.SelectedOptionId);
                var correctOption = q.Options.FirstOrDefault(o => o.IsCorrect);

                return new
                {
                    QuestionId = q.QuestionId,
                    QuestionText = q.QuestionText,
                    SelectedOptionId = studentAnswer?.SelectedOptionId,
                    SelectedOptionText = selectedOption?.Text,
                    CorrectOptionId = correctOption?.OptionId,
                    CorrectOptionText = correctOption?.Text,
                    IsCorrect = selectedOption?.IsCorrect ?? false,
                    AssessmentId = assessment.AssessmentId,
                    StudentId = studentId,
                    AllOptions = q.Options.Select(o => new
                    {
                        o.OptionId,
                        o.Text,
                        o.IsCorrect
                    })
                };
            }).ToList();

            int totalQuestions = answers.Count;
            int correctAnswers = answers.Count(a => a.IsCorrect);

            return Ok(new
            {
                AssessmentId = assessment.AssessmentId,
                AssessmentTitle = assessment.Title,
                StudentId = studentId,
                TotalQuestions = totalQuestions,
                CorrectAnswers = correctAnswers,
                Score = $"{correctAnswers}/{totalQuestions}",
                AttemptDate = result.AttemptDate,
                Answers = answers
            });
        }
    }
}

