using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backendapi.Controllers;
using Backendapi.Data;
using Backendapi.Models;
using Backendapi.Services;
using finalpracticeproject.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Backendapi.Tests
{
    [TestFixture]
    public class AssessmentsControllerTests
    {
        private AssessmentsController _controller;
        private AppDbContext _context;
        private Mock<ILogger<AssessmentsController>> _loggerMock;
        private Mock<ITelemetryService> _telemetryMock;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _loggerMock = new Mock<ILogger<AssessmentsController>>();
            _telemetryMock = new Mock<ITelemetryService>();

            _controller = new AssessmentsController(_context, _loggerMock.Object, _telemetryMock.Object);
        }

        [TearDown]
        public void Cleanup()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Test]
        public async Task GetAssessments_ReturnsEmptyList_WhenNoAssessmentsExist()
        {
            var result = await _controller.GetAssessments();
            var assessments = result.Value;

            Assert.That(assessments, Is.Not.Null);
            Assert.That(assessments, Is.Empty);
        }

        [Test]
        public async Task PostAssessment_CreatesNewAssessment()
        {
            var assessmentDto = new AssessmentCreateDto
            {
                AssessmentId = Guid.NewGuid(),
                Title = "Sample Test",
                CourseId = Guid.NewGuid(),
                MaxScore = 10,
                Questions = new List<QuestionCreateDto>
                {
                    new QuestionCreateDto
                    {
                        QuestionId = Guid.NewGuid(),
                        QuestionText = "What is 2 + 2?",
                        Options = new List<OptionCreateDto>
                        {
                            new OptionCreateDto { OptionId = Guid.NewGuid(), Text = "4", IsCorrect = true },
                            new OptionCreateDto { OptionId = Guid.NewGuid(), Text = "3", IsCorrect = false }
                        }
                    }
                }
            };

            var result = await _controller.PostAssessment(assessmentDto);
            var createdResult = result.Result as CreatedAtActionResult;

            Assert.That(createdResult, Is.Not.Null);
            Assert.That(createdResult.ActionName, Is.EqualTo("GetAssessment"));

            var createdAssessment = await _context.Assessments
                .Include(a => a.Questions)
                .FirstOrDefaultAsync(a => a.AssessmentId == assessmentDto.AssessmentId);

            Assert.That(createdAssessment, Is.Not.Null);
            Assert.That(createdAssessment.Title, Is.EqualTo("Sample Test"));
        }

        [Test]
        public async Task GetAssessment_ReturnsNotFound_WhenAssessmentDoesNotExist()
        {
            var nonExistentId = Guid.NewGuid();
            var result = await _controller.GetAssessment(nonExistentId);

            Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
        }

        [Test]
        public async Task DeleteAssessment_ReturnsNotFound_WhenAssessmentNotExists()
        {
            var result = await _controller.DeleteAssessment(Guid.NewGuid());
            var notFound = result as NotFoundObjectResult;

            Assert.That(notFound, Is.Not.Null);
            Assert.That(notFound.StatusCode, Is.EqualTo(404));
        }

        [Test]
        public async Task PutAssessment_ReturnsBadRequest_WhenIdMismatch()
        {
            var dto = new AssessmentCreateDto
            {
                AssessmentId = Guid.NewGuid(),
                Title = "Mismatch Test",
                CourseId = Guid.NewGuid(),
                MaxScore = 100,
                Questions = new List<QuestionCreateDto>()
            };

            var result = await _controller.PutAssessment(Guid.NewGuid(), dto);

            Assert.That(result, Is.TypeOf<BadRequestResult>());
        }

        [Test]
        public async Task GetAssessmentsByCourse_ReturnsNotFound_WhenCourseDoesNotExist()
        {
            var result = await _controller.GetAssessmentsByCourse(Guid.NewGuid());
            var notFound = result.Result as NotFoundObjectResult;

            Assert.That(notFound, Is.Not.Null);
            Assert.That(notFound.StatusCode, Is.EqualTo(404));
        }

        [Test]
        public async Task GetAssessmentResult_ReturnsNotFound_WhenAssessmentDoesNotExist()
        {
            var result = await _controller.GetAssessmentResult(Guid.NewGuid(), Guid.NewGuid());
            var notFound = result.Result as NotFoundObjectResult;

            Assert.That(notFound, Is.Not.Null);
        }

        [Test]
        public async Task GetAssessmentResult_ReturnsNotFound_WhenResultNotFound()
        {
            var assessmentId = Guid.NewGuid();
            var studentId = Guid.NewGuid();

            _context.Assessments.Add(new Assessment
            {
                AssessmentId = assessmentId,
                Title = "Mock Test",
                MaxScore = 10,
                Questions = new List<Question>()
            });

            await _context.SaveChangesAsync();

            var result = await _controller.GetAssessmentResult(assessmentId, studentId);
            var notFound = result.Result as NotFoundObjectResult;

            Assert.That(notFound, Is.Not.Null);
            Assert.That(notFound.StatusCode, Is.EqualTo(404));
        }
    }
}

