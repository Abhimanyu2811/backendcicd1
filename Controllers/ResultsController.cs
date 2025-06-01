using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Backendapi.Data;
using finalpracticeproject.DTOs;
using Backendapi.Models;
using Microsoft.AspNetCore.Authorization;
using System.Security.Claims;
using Microsoft.Extensions.Logging;
using System.Text.Json;

namespace finalpracticeproject.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class ResultsController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ResultsController> _logger;

        public ResultsController(AppDbContext context, ILogger<ResultsController> logger)
        {
            _context = context;
            _logger = logger;
        }

        // GET: api/Results
        [HttpGet]
        [Authorize(Roles = "Instructor")]
        public async Task<ActionResult<IEnumerable<Result>>> GetResults()
        {
            return await _context.Results.ToListAsync();
        }

        // GET: api/Results/5
        [HttpGet("{id}")]
        public async Task<ActionResult<Result>> GetResult(Guid? id)
        {
            var result = await _context.Results.FindAsync(id);
            if (result == null) return NotFound();

            var userId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (userId != result.UserId.ToString() && !User.IsInRole("Instructor")) return Forbid();

            return result;
        }

        // PUT: api/Results/5
        [HttpPut("{id}")]
        public async Task<IActionResult> PutResult(Guid id, ResultCreateDto resultDto)
        {
            if (id != resultDto.ResultId) return BadRequest();

            var result = await _context.Results.FindAsync(id);
            if (result == null) return NotFound();

            result.Score = (int)resultDto.Score;
            result.UserId = (Guid)resultDto.UserId;
            result.AssessmentId = (Guid)resultDto.AssessmentId;
            result.AttemptDate = resultDto.AttemptDate;

            await _context.SaveChangesAsync();
            return NoContent();
        }

        // POST: api/Results
        [HttpPost]
        public async Task<ActionResult<Result>> PostResult(ResultCreateDto resultDto)
        {
            var result = new Result
            {
                ResultId = resultDto.ResultId,
                AssessmentId = (Guid)resultDto.AssessmentId,
                UserId = (Guid)resultDto.UserId,
                Score = (int)resultDto.Score,
                AttemptDate = resultDto.AttemptDate
            };

            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            return CreatedAtAction("GetResult", new { id = result.ResultId }, result);
        }

        // POST: api/Results/submit
        [HttpPost("submit")]
        public async Task<ActionResult<object>> SubmitAssessment([FromBody] AssessmentSubmissionDto submission)
        {
            _logger.LogInformation($"Received submission: {JsonSerializer.Serialize(submission)}");

            var assessment = await _context.Assessments
                .Include(a => a.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(a => a.AssessmentId == submission.AssessmentId);

            if (assessment == null)
            {
                _logger.LogWarning($"Assessment not found: {submission.AssessmentId}");
                return NotFound("Assessment not found");
            }

            var user = await _context.Users.FindAsync(submission.UserId);
            if (user == null)
            {
                _logger.LogWarning($"User not found: {submission.UserId}");
                return NotFound("User not found");
            }

            var result = new Result
            {
                ResultId = Guid.NewGuid(),
                AssessmentId = assessment.AssessmentId,
                UserId = user.UserId,
                AttemptDate = DateTime.UtcNow,
                StudentAnswers = new List<StudentAnswer>()
            };

            _logger.LogInformation($"Processing {submission.Answers.Count()} answers");

            int score = 0;
            foreach (var answer in submission.Answers)
            {
                var question = assessment.Questions.FirstOrDefault(q => q.QuestionId == answer.QuestionId);
                if (question == null)
                {
                    _logger.LogWarning($"Question not found: {answer.QuestionId}");
                    continue;
                }

                var selectedOption = question.Options.FirstOrDefault(o => o.OptionId == answer.SelectedOptionId);
                if (selectedOption == null)
                {
                    _logger.LogWarning($"Selected option not found: {answer.SelectedOptionId}");
                    continue;
                }

                if (selectedOption.IsCorrect)
                {
                    score++;
                    _logger.LogInformation($"Correct answer for question {question.QuestionId}");
                }

                var studentAnswer = new StudentAnswer
                {
                    AnswerId = Guid.NewGuid(),
                    ResultId = result.ResultId,
                    QuestionId = question.QuestionId,
                    SelectedOptionId = selectedOption.OptionId,
                    AssessmentId = assessment.AssessmentId,
                    StudentId = user.UserId
                };

                _logger.LogInformation($"Created student answer: {JsonSerializer.Serialize(studentAnswer)}");
                result.StudentAnswers.Add(studentAnswer);
            }

            result.Score = score;
            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            var response = new
            {
                ResultId = result.ResultId,
                Score = result.Score,
                AttemptDate = result.AttemptDate,
                StudentAnswers = result.StudentAnswers.Select(sa => new
                {
                    sa.QuestionId,
                    sa.SelectedOptionId,
                    sa.AssessmentId,
                    sa.StudentId
                })
            };

            _logger.LogInformation($"Returning response: {JsonSerializer.Serialize(response)}");
            return Ok(response);
        }

        // DELETE: api/Results/5
        [HttpDelete("{id}")]
        public async Task<IActionResult> DeleteResult(Guid id)
        {
            var result = await _context.Results.FindAsync(id);
            if (result == null) return NotFound();

            _context.Results.Remove(result);
            await _context.SaveChangesAsync();

            return NoContent();
        }

        // GET: api/Results/student/{userId}/assessment/{assessmentId}
        [HttpGet("student/{userId}/assessment/{assessmentId}")]
        public async Task<ActionResult<object>> GetStudentAssessmentResult(Guid userId, Guid assessmentId)
        {
            _logger.LogInformation($"Getting assessment result for User: {userId}, Assessment: {assessmentId}");

            var currentUserId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (currentUserId != userId.ToString() && !User.IsInRole("Instructor"))
            {
                _logger.LogWarning($"Unauthorized access attempt. CurrentUser: {currentUserId}, RequestedUser: {userId}");
                return Forbid();
            }

            var result = await _context.Results
                .Include(r => r.StudentAnswers)
                .Include(r => r.Assessment)
                    .ThenInclude(a => a.Questions)
                        .ThenInclude(q => q.Options)
                .AsNoTracking()
                .FirstOrDefaultAsync(r => r.UserId == userId && r.AssessmentId == assessmentId);

            if (result == null)
            {
                _logger.LogWarning($"No result found for User: {userId}, Assessment: {assessmentId}");
                return NotFound("No submission found for this assessment");
            }

            var user = await _context.Users.FindAsync(userId);
            if (user == null)
            {
                _logger.LogWarning($"User not found: {userId}");
                return NotFound("User not found");
            }

            _logger.LogInformation($"Found result with {result.StudentAnswers.Count} answers");

            // Process each question to include both selected and correct options
            var processedAnswers = result.Assessment.Questions.Select(question =>
            {
                var studentAnswer = result.StudentAnswers.FirstOrDefault(sa => sa.QuestionId == question.QuestionId);
                var selectedOption = question.Options.FirstOrDefault(o => o.OptionId == studentAnswer?.SelectedOptionId);
                var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect);

                _logger.LogInformation($"Processing question {question.QuestionId}:");
                _logger.LogInformation($"- Selected Option: {selectedOption?.OptionId} - {selectedOption?.Text}");
                _logger.LogInformation($"- Correct Option: {correctOption?.OptionId} - {correctOption?.Text}");

                return new
                {
                    QuestionId = question.QuestionId,
                    QuestionText = question.QuestionText,
                    StudentAnswer = studentAnswer != null ? new
                    {
                        AnswerId = studentAnswer.AnswerId,
                        SelectedOptionId = studentAnswer.SelectedOptionId,
                        SelectedOptionText = selectedOption?.Text
                    } : null,
                    CorrectOption = new
                    {
                        OptionId = correctOption?.OptionId,
                        Text = correctOption?.Text
                    },
                    IsCorrect = selectedOption?.IsCorrect ?? false,
                    AllOptions = question.Options.Select(o => new
                    {
                        OptionId = o.OptionId,
                        Text = o.Text,
                        IsCorrect = o.IsCorrect,
                        IsSelected = o.OptionId == studentAnswer?.SelectedOptionId
                    }).ToList()
                };
            }).ToList();

            var response = new
            {
                ResultId = result.ResultId,
                UserName = user.Name,
                AssessmentTitle = result.Assessment?.Title,
                Score = result.Score,
                MaxScore = result.Assessment?.Questions.Count ?? 0,
                AttemptDate = result.AttemptDate,
                Questions = processedAnswers
            };

            _logger.LogInformation($"Returning response: {JsonSerializer.Serialize(response)}");
            return Ok(response);
        }

        // GET: api/Results/assessment/{assessmentId}/submissions
        [HttpGet("assessment/{assessmentId}/submissions")]
        public async Task<ActionResult<IEnumerable<object>>> GetAssessmentSubmissions(Guid assessmentId)
        {
            _logger.LogInformation($"Getting submissions for Assessment: {assessmentId}");

            var assessment = await _context.Assessments
                .Include(a => a.Questions)
                    .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(a => a.AssessmentId == assessmentId);

            if (assessment == null)
            {
                _logger.LogWarning($"Assessment not found: {assessmentId}");
                return NotFound("Assessment not found");
            }

            var submissions = await _context.Results
                .Include(r => r.StudentAnswers)
                .Include(r => r.User)
                .Include(r => r.Assessment)
                    .ThenInclude(a => a.Questions)
                        .ThenInclude(q => q.Options)
                .Where(r => r.AssessmentId == assessmentId)
                .OrderByDescending(r => r.AttemptDate)
                .ToListAsync();

            _logger.LogInformation($"Found {submissions.Count} submissions");

            var response = submissions.Select(s =>
            {
                var processedQuestions = assessment.Questions.Select(question =>
                {
                    var studentAnswer = s.StudentAnswers.FirstOrDefault(sa => sa.QuestionId == question.QuestionId);
                    var selectedOption = question.Options.FirstOrDefault(o => o.OptionId == studentAnswer?.SelectedOptionId);
                    var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect);

                    return new
                    {
                        QuestionId = question.QuestionId,
                        QuestionText = question.QuestionText,
                        StudentAnswer = new
                        {
                            SelectedOptionId = studentAnswer?.SelectedOptionId,
                            SelectedOptionText = selectedOption?.Text,
                            IsCorrect = selectedOption?.IsCorrect ?? false
                        },
                        CorrectAnswer = new
                        {
                            OptionId = correctOption?.OptionId,
                            Text = correctOption?.Text
                        },
                        AllOptions = question.Options.Select(o => new
                        {
                            OptionId = o.OptionId,
                            Text = o.Text,
                            IsCorrect = o.IsCorrect,
                            IsSelected = o.OptionId == studentAnswer?.SelectedOptionId
                        }).ToList()
                    };
                }).ToList();

                return new
                {
                    ResultId = s.ResultId,
                    UserId = s.UserId,
                    UserName = s.User?.Name,
                    Score = s.Score,
                    MaxScore = assessment.Questions.Count,
                    AttemptDate = s.AttemptDate,
                    Percentage = assessment.Questions.Count > 0
                        ? Math.Round((double)s.Score / assessment.Questions.Count * 100, 2)
                        : 0,
                    Questions = processedQuestions
                };
            });

            _logger.LogInformation("Returning processed submissions");
            return Ok(response);
        }

        // GET: api/Results/assessment/{assessmentId}/submission/{resultId}
        [HttpGet("assessment/{assessmentId}/submission/{resultId}")]
        [Authorize(Roles = "Instructor")]
        public async Task<ActionResult<object>> GetDetailedSubmission(Guid assessmentId, Guid resultId)
        {
            _logger.LogInformation($"Getting detailed submission for Assessment: {assessmentId}, Result: {resultId}");

            var submission = await _context.Results
                .Include(r => r.StudentAnswers)
                .Include(r => r.User)
                .Include(r => r.Assessment)
                    .ThenInclude(a => a.Questions)
                        .ThenInclude(q => q.Options)
                .FirstOrDefaultAsync(r => r.ResultId == resultId && r.AssessmentId == assessmentId);

            if (submission == null)
            {
                _logger.LogWarning($"Submission not found for Assessment: {assessmentId}, Result: {resultId}");
                return NotFound("Submission not found");
            }

            _logger.LogInformation($"Processing submission with {submission.StudentAnswers.Count} answers");

            var processedQuestions = submission.Assessment.Questions.Select(question =>
            {
                var studentAnswer = submission.StudentAnswers.FirstOrDefault(sa => sa.QuestionId == question.QuestionId);
                var selectedOption = question.Options.FirstOrDefault(o => o.OptionId == studentAnswer?.SelectedOptionId);
                var correctOption = question.Options.FirstOrDefault(o => o.IsCorrect);

                _logger.LogInformation($"Processing question {question.QuestionId}:");
                _logger.LogInformation($"- Selected Option: {selectedOption?.OptionId} - {selectedOption?.Text}");
                _logger.LogInformation($"- Correct Option: {correctOption?.OptionId} - {correctOption?.Text}");

                return new
                {
                    QuestionId = question.QuestionId,
                    QuestionText = question.QuestionText,
                    StudentAnswer = new
                    {
                        SelectedOptionId = studentAnswer?.SelectedOptionId,
                        SelectedOptionText = selectedOption?.Text,
                        IsCorrect = selectedOption?.IsCorrect ?? false
                    },
                    CorrectAnswer = new
                    {
                        OptionId = correctOption?.OptionId,
                        Text = correctOption?.Text
                    },
                    AllOptions = question.Options.Select(o => new
                    {
                        OptionId = o.OptionId,
                        Text = o.Text,
                        IsCorrect = o.IsCorrect,
                        IsSelected = o.OptionId == studentAnswer?.SelectedOptionId
                    }).ToList()
                };
            }).ToList();

            var response = new
            {
                ResultId = submission.ResultId,
                UserId = submission.UserId,
                UserName = submission.User?.Name,
                AssessmentTitle = submission.Assessment?.Title,
                Score = submission.Score,
                MaxScore = submission.Assessment?.Questions.Count ?? 0,
                Percentage = submission.Assessment?.Questions.Count > 0
                    ? Math.Round((double)submission.Score / submission.Assessment.Questions.Count * 100, 2)
                    : 0,
                AttemptDate = submission.AttemptDate,
                Questions = processedQuestions
            };

            _logger.LogInformation($"Returning response: {JsonSerializer.Serialize(response)}");
            return Ok(response);
        }

        private bool ResultExists(Guid? id)
        {
            return _context.Results.Any(e => e.ResultId == id);
        }
    }
}
