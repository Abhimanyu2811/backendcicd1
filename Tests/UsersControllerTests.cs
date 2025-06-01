using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using finalpracticeproject.Controllers;
using finalpracticeproject.DTOs;
using Backendapi.Data;
using Backendapi.Models;
using Backendapi.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Moq;
using NUnit.Framework;

namespace finalpracticeproject.Tests
{
    [TestFixture]
    public class UsersControllerTests
    {
        private AppDbContext _context;
        private UsersController _controller;
        private Mock<ILogger<UsersController>> _loggerMock;
        private Mock<ITelemetryService> _telemetryMock;

        [SetUp]
        public void SetUp()
        {
            var options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;

            _context = new AppDbContext(options);
            _loggerMock = new Mock<ILogger<UsersController>>();
            _telemetryMock = new Mock<ITelemetryService>();
            _controller = new UsersController(_context, _loggerMock.Object, _telemetryMock.Object);
        }

        [TearDown]
        public void TearDown()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }

        [Test]
        public async Task GetUsers_ReturnsAllUsers()
        {
            // Arrange
            var user1 = new User { UserId = Guid.NewGuid(), Name = "User1", Email = "u1@example.com", Role = "Student" };
            var user2 = new User { UserId = Guid.NewGuid(), Name = "User2", Email = "u2@example.com", Role = "Instructor" };
            await _context.Users.AddRangeAsync(user1, user2);
            await _context.SaveChangesAsync();

            // Act
            var result = await _controller.GetUsers();

            // Assert
            Assert.That(result.Value, Has.Count.EqualTo(2));
        }

        [Test]
        public async Task GetUser_ExistingId_ReturnsUser()
        {
            var userId = Guid.NewGuid();
            var user = new User { UserId = userId, Name = "TestUser", Email = "test@example.com", Role = "Student" };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var actionResult = await _controller.GetUser(userId);

            Assert.That(actionResult.Value, Is.Not.Null);
            Assert.That(actionResult.Value.UserId, Is.EqualTo(userId));
        }

        [Test]
        public async Task GetUser_NonExistingId_ReturnsNotFound()
        {
            var result = await _controller.GetUser(Guid.NewGuid());
            Assert.That(result.Result, Is.TypeOf<NotFoundResult>());
        }

        [Test]
        public async Task PostUser_ValidUser_ReturnsCreatedUser()
        {
            var userDto = new UserCreateDto
            {
                UserId = Guid.NewGuid(),
                Name = "New User",
                Email = "newuser@example.com",
                Role = "Student",
                PasswordHash = "hashedpassword",
                CourseIds = null
            };

            var actionResult = await _controller.PostUser(userDto);
            var createdAtActionResult = actionResult.Result as CreatedAtActionResult;

            Assert.That(createdAtActionResult, Is.Not.Null);
            var createdUser = createdAtActionResult.Value as User;
            Assert.That(createdUser, Is.Not.Null);
            Assert.That(createdUser.Email, Is.EqualTo(userDto.Email));

            // Verify user saved in DB
            var userInDb = await _context.Users.FindAsync(userDto.UserId);
            Assert.That(userInDb, Is.Not.Null);
            Assert.That(userInDb.Email, Is.EqualTo(userDto.Email));
        }

        [Test]
        public async Task PutUser_ValidUpdate_ReturnsNoContent()
        {
            var userId = Guid.NewGuid();
            var existingUser = new User
            {
                UserId = userId,
                Name = "Old Name",
                Email = "oldemail@example.com",
                Role = "Student",
                PasswordHash = "oldhash"
            };
            await _context.Users.AddAsync(existingUser);
            await _context.SaveChangesAsync();

            var updateDto = new UserCreateDto
            {
                UserId = userId,
                Name = "Updated Name",
                Email = "newemail@example.com",
                Role = "Instructor",
                PasswordHash = "newhash",
                CourseIds = null
            };

            var result = await _controller.PutUser(userId, updateDto);

            Assert.That(result, Is.TypeOf<NoContentResult>());

            var updatedUser = await _context.Users.FindAsync(userId);
            Assert.That(updatedUser.Name, Is.EqualTo(updateDto.Name));
            Assert.That(updatedUser.Email, Is.EqualTo(updateDto.Email));
            Assert.That(updatedUser.Role, Is.EqualTo(updateDto.Role));
        }

        [Test]
        public async Task PutUser_IdMismatch_ReturnsBadRequest()
        {
            var userId = Guid.NewGuid();
            var updateDto = new UserCreateDto
            {
                UserId = Guid.NewGuid(), // different from userId param
                Name = "Name",
                Email = "email@example.com",
                Role = "Student",
                PasswordHash = "hash"
            };

            var result = await _controller.PutUser(userId, updateDto);
            Assert.That(result, Is.TypeOf<BadRequestResult>());
        }

        [Test]
        public async Task PutUser_NonExistingUser_ReturnsNotFound()
        {
            var userId = Guid.NewGuid();
            var updateDto = new UserCreateDto
            {
                UserId = userId,
                Name = "Name",
                Email = "email@example.com",
                Role = "Student",
                PasswordHash = "hash"
            };

            var result = await _controller.PutUser(userId, updateDto);
            Assert.That(result, Is.TypeOf<NotFoundResult>());
        }

        [Test]
        public async Task DeleteUser_ExistingUser_ReturnsNoContent()
        {
            var userId = Guid.NewGuid();
            var user = new User { UserId = userId, Name = "ToDelete", Email = "del@example.com", Role = "Student" };
            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var result = await _controller.DeleteUser(userId);

            Assert.That(result, Is.TypeOf<NoContentResult>());
            Assert.That(await _context.Users.FindAsync(userId), Is.Null);
        }

        [Test]
        public async Task DeleteUser_NonExistingUser_ReturnsNotFound()
        {
            var result = await _controller.DeleteUser(Guid.NewGuid());
            Assert.That(result, Is.TypeOf<NotFoundResult>());
        }
    }
}
