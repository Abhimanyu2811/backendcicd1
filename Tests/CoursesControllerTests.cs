﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Backendapi.Controllers;
using Backendapi.Data;
using Backendapi.Models;
using Backendapi.Services;
using finalpracticeproject.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace Backendapi.Tests
{
    [TestFixture]
    public class CoursesControllerTests
    {
        private Mock<AppDbContext> _mockContext;
        private Mock<ILogger<CoursesController>> _mockLogger;
        private Mock<ITelemetryService> _mockTelemetry;
        private CoursesController _controller;
        private List<Course> _courses;
        private List<User> _users;

        [SetUp]
        public void Setup()
        {
            _mockContext = new Mock<AppDbContext>();
            _mockLogger = new Mock<ILogger<CoursesController>>();
            _mockTelemetry = new Mock<ITelemetryService>();

            // Sample data
            _courses = new List<Course>
            {
                new Course
                {
                    CourseId = Guid.NewGuid(),
                    Title = "Course 1",
                    Description = "Description 1",
                    InstructorId = Guid.NewGuid(),
                    MediaUrl = "media1",
                    CourseUrl = "url1",
                    Instructor = new User { UserId = Guid.NewGuid(), Name = "Instructor 1", Email = "inst1@example.com" }
                },
                new Course
                {
                    CourseId = Guid.NewGuid(),
                    Title = "Course 2",
                    Description = "Description 2",
                    InstructorId = Guid.NewGuid(),
                    MediaUrl = "media2",
                    CourseUrl = "url2",
                    Instructor = new User { UserId = Guid.NewGuid(), Name = "Instructor 2", Email = "inst2@example.com" }
                }
            };

            _users = new List<User>
            {
                new User
                {
                    UserId = Guid.NewGuid(),
                    Name = "User 1",
                    Email = "user1@example.com",
                    EnrolledCourses = new List<Course>()
                }
            };

            // Setup mock DbSet for Courses
            var mockCoursesDbSet = GetQueryableMockDbSet(_courses);
            _mockContext.Setup(c => c.Courses).Returns(mockCoursesDbSet.Object);

            // Setup mock DbSet for Users
            var mockUsersDbSet = GetQueryableMockDbSet(_users);
            _mockContext.Setup(c => c.Users).Returns(mockUsersDbSet.Object);

            _controller = new CoursesController(_mockContext.Object, _mockLogger.Object, _mockTelemetry.Object);
        }

        // Helper to mock DbSet<T> with IQueryable support
        private static Mock<DbSet<T>> GetQueryableMockDbSet<T>(List<T> sourceList) where T : class
        {
            var queryable = sourceList.AsQueryable();
            var dbSet = new Mock<DbSet<T>>();
            dbSet.As<IQueryable<T>>().Setup(m => m.Provider).Returns(queryable.Provider);
            dbSet.As<IQueryable<T>>().Setup(m => m.Expression).Returns(queryable.Expression);
            dbSet.As<IQueryable<T>>().Setup(m => m.ElementType).Returns(queryable.ElementType);
            dbSet.As<IQueryable<T>>().Setup(m => m.GetEnumerator()).Returns(queryable.GetEnumerator());
            dbSet.Setup(d => d.Add(It.IsAny<T>())).Callback<T>(sourceList.Add);
            return dbSet;
        }



        [Test]
        public async Task PostCourse_ValidDto_CreatesCourse()
        {
            var newCourseDto = new CourseCreateDto
            {
                CourseId = Guid.NewGuid(),
                Title = "New Course",
                Description = "New Desc",
                InstructorId = Guid.NewGuid(),
                MediaUrl = "new media",
                CourseUrl = "https://youtube.com/watch?v=abc123"
            };

            // Mock context save
            _mockContext.Setup(c => c.SaveChangesAsync(default)).ReturnsAsync(1);

            var result = await _controller.PostCourse(newCourseDto);
            Assert.That(result.Result, Is.InstanceOf<CreatedAtActionResult>());

            var createdResult = (CreatedAtActionResult)result.Result;
            var createdCourse = createdResult.Value as dynamic;

            Assert.That(createdCourse.title, Is.EqualTo(newCourseDto.Title));
            Assert.That(createdCourse.courseUrl, Does.Contain("youtube.com/embed/abc123"));
        }

        [Test]
        public async Task PutCourse_IdMismatch_ReturnsBadRequest()
        {
            var courseId = Guid.NewGuid();
            var dto = new CourseCreateDto { CourseId = Guid.NewGuid() };

            var result = await _controller.PutCourse(courseId, dto);
            Assert.That(result, Is.InstanceOf<BadRequestObjectResult>());
        }



        [Test]
        public async Task DeleteCourse_ExistingId_ReturnsNoContent()
        {
            var existingCourse = _courses[0];

            _mockContext.Setup(c => c.Courses.FindAsync(existingCourse.CourseId))
                .ReturnsAsync(existingCourse);
            _mockContext.Setup(c => c.SaveChangesAsync(default)).ReturnsAsync(1);

            var result = await _controller.DeleteCourse(existingCourse.CourseId);
            Assert.That(result, Is.InstanceOf<NoContentResult>());
        }

        [Test]
        public async Task DeleteCourse_NonExistingId_ReturnsNotFound()
        {
            _mockContext.Setup(c => c.Courses.FindAsync(It.IsAny<Guid>())).ReturnsAsync((Course)null);

            var result = await _controller.DeleteCourse(Guid.NewGuid());
            Assert.That(result, Is.InstanceOf<NotFoundResult>());
        }




    }
}
