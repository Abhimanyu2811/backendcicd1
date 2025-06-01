using NUnit.Framework;
using Moq;
using Backendapi.Controllers;
using Backendapi.Models;
using Backendapi.Data;
using Backendapi.Services;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Threading.Tasks;
using System.Collections.Generic;
using System.Linq;
using System;
using System.Text;
using Microsoft.IdentityModel.Tokens;

namespace Backendapi.Tests
{
    [TestFixture]
    public class AuthControllerTests
    {
        private AuthController _controller;
        private Mock<AppDbContext> _mockContext;
        private Mock<IConfiguration> _mockConfig;
        private Mock<ITelemetryService> _mockTelemetry;
        private List<User> _userList;
        private DbContextOptions<AppDbContext> _options;

        [SetUp]
        public void SetUp()
        {
            _options = new DbContextOptionsBuilder<AppDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            var context = new AppDbContext(_options);

            _mockContext = new Mock<AppDbContext>(_options);
            _mockConfig = new Mock<IConfiguration>();
            _mockTelemetry = new Mock<ITelemetryService>();

            _mockConfig.Setup(c => c["Jwt:Key"]).Returns("supersecretkeysupersecretkey");
            _mockConfig.Setup(c => c["Jwt:Issuer"]).Returns("TestIssuer");
            _mockConfig.Setup(c => c["Jwt:Audience"]).Returns("TestAudience");

            _controller = new AuthController(context, _mockConfig.Object, _mockTelemetry.Object);
        }

        [Test]
        public async Task Register_ReturnsOk_WhenUserIsNew()
        {
            var userDto = new UserDto
            {
                Name = "Test",
                Email = "test@example.com",
                Password = "Password123!",
                Role = "User"
            };

            var result = await _controller.Register(userDto);

            Assert.That(result.Result, Is.TypeOf<OkObjectResult>());
        }

        [Test]
        public async Task Register_ReturnsBadRequest_WhenEmailAlreadyExists()
        {
            var context = new AppDbContext(_options);
            context.Users.Add(new User
            {
                UserId = Guid.NewGuid(),
                Name = "Existing",
                Email = "existing@example.com",
                PasswordHash = Convert.ToBase64String(new byte[64]),
                PasswordSalt = Convert.ToBase64String(new byte[64]),
                Role = "User"
            });
            context.SaveChanges();

            var controller = new AuthController(context, _mockConfig.Object, _mockTelemetry.Object);
            var userDto = new UserDto
            {
                Name = "Duplicate",
                Email = "existing@example.com",
                Password = "Password123!",
                Role = "User"
            };

            var result = await controller.Register(userDto);

            Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
        }

      

        [Test]
        public async Task Login_ReturnsBadRequest_WhenUserNotFound()
        {
            var userDto = new UserDto
            {
                Email = "nonexistent@example.com",
                Password = "AnyPassword"
            };

            var result = await _controller.Login(userDto);

            Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
        }

        [Test]
        public async Task Login_ReturnsBadRequest_WhenWrongPassword()
        {
            var context = new AppDbContext(_options);
            var user = new User
            {
                UserId = Guid.NewGuid(),
                Name = "User",
                Email = "wrongpass@example.com",
                PasswordHash = Convert.ToBase64String(new byte[64]),
                PasswordSalt = Convert.ToBase64String(new byte[64]),
                Role = "User"
            };
            context.Users.Add(user);
            context.SaveChanges();

            var controller = new AuthController(context, _mockConfig.Object, _mockTelemetry.Object);
            var userDto = new UserDto
            {
                Email = "wrongpass@example.com",
                Password = "WrongPassword"
            };

            var result = await controller.Login(userDto);

            Assert.That(result.Result, Is.TypeOf<BadRequestObjectResult>());
        }

        private void CreatePasswordHash(string password, out byte[] passwordHash, out byte[] passwordSalt)
        {
            using (var hmac = new System.Security.Cryptography.HMACSHA512())
            {
                passwordSalt = hmac.Key;
                passwordHash = hmac.ComputeHash(Encoding.UTF8.GetBytes(password));
            }
        }
    }
}
