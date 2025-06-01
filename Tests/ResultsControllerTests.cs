using NUnit.Framework;
using Moq;
using Microsoft.Extensions.Logging;
using Backendapi.Controllers;
using Backendapi.Data;
using Backendapi.Models;
using finalpracticeproject.DTOs;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using finalpracticeproject.Controllers;

namespace Backendapi.Tests
{
    [TestFixture]
    public class ResultsControllerTests
    {
        private ResultsController _controller;
        private Mock<ILogger<ResultsController>> _mockLogger;
        private AppDbContext _context;

        [SetUp]
        public void Setup()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _mockLogger = new Mock<ILogger<ResultsController>>();
            _controller = new ResultsController(_context, _mockLogger.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        private void SetUserContext(string userId, string role = "Student")
        {
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, userId),
                new Claim(ClaimTypes.Role, role)
            };

            var identity = new ClaimsIdentity(claims, "TestAuth");
            var principal = new ClaimsPrincipal(identity);

            _controller.ControllerContext = new ControllerContext
            {
                HttpContext = new DefaultHttpContext { User = principal }
            };
        }

     

        [Test]
        public async Task GetResult_ShouldReturnResult_ForAuthorizedStudent()
        {
            // Arrange
            var userId = Guid.NewGuid();
            var resultId = Guid.NewGuid();

            var result = new Result
            {
                ResultId = resultId,
                UserId = userId,
                AssessmentId = Guid.NewGuid(),
                Score = 5,
                AttemptDate = DateTime.UtcNow
            };
            _context.Results.Add(result);
            await _context.SaveChangesAsync();

            SetUserContext(userId.ToString(), "Student");

            // Act
            var response = await _controller.GetResult(resultId);

            // Assert
            Assert.That(response.Value, Is.Not.Null);
            Assert.That(response.Value.Score, Is.EqualTo(5));
        }

        [Test]
        public async Task GetResult_ShouldReturnForbid_WhenUnauthorized()
        {
            var userId = Guid.NewGuid();
            var anotherUserId = Guid.NewGuid();
            var resultId = Guid.NewGuid();

            _context.Results.Add(new Result
            {
                ResultId = resultId,
                UserId = userId,
                AssessmentId = Guid.NewGuid(),
                Score = 3,
                AttemptDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            SetUserContext(anotherUserId.ToString(), "Student");

            // Act
            var response = await _controller.GetResult(resultId);

            // Assert
            Assert.That(response.Result, Is.TypeOf<ForbidResult>());
        }

        [Test]
        public async Task DeleteResult_ShouldRemoveResult()
        {
            // Arrange
            var resultId = Guid.NewGuid();
            _context.Results.Add(new Result
            {
                ResultId = resultId,
                UserId = Guid.NewGuid(),
                AssessmentId = Guid.NewGuid(),
                Score = 2,
                AttemptDate = DateTime.UtcNow
            });
            await _context.SaveChangesAsync();

            // Act
            var response = await _controller.DeleteResult(resultId);

            // Assert
            Assert.That(response, Is.TypeOf<NoContentResult>());
            Assert.That(await _context.Results.FindAsync(resultId), Is.Null);
        }

        [Test]
        public async Task PutResult_ShouldUpdateResult()
        {
            // Arrange
            var resultId = Guid.NewGuid();
            var original = new Result
            {
                ResultId = resultId,
                UserId = Guid.NewGuid(),
                AssessmentId = Guid.NewGuid(),
                Score = 1,
                AttemptDate = DateTime.UtcNow
            };
            _context.Results.Add(original);
            await _context.SaveChangesAsync();

            var updatedDto = new ResultCreateDto
            {
                ResultId = resultId,
                UserId = original.UserId,
                AssessmentId = original.AssessmentId,
                Score = 10,
                AttemptDate = DateTime.UtcNow
            };

            // Act
            var result = await _controller.PutResult(resultId, updatedDto);

            // Assert
            var updated = await _context.Results.FindAsync(resultId);
            Assert.That(result, Is.TypeOf<NoContentResult>());
            Assert.That(updated.Score, Is.EqualTo(10));
        }
    }
}
