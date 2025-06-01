using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Backendapi.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace Backendapi.Data
{
    public static class DataSeeder
    {
        public static async Task SeedData(IServiceProvider serviceProvider)
        {
            using var context = new AppDbContext(
                serviceProvider.GetRequiredService<DbContextOptions<AppDbContext>>());

            // Check if data already exists
            if (await context.Assessments.AnyAsync())
            {
                return; // Database has been seeded
            }

            // Create sample instructor
            var instructor = new User
            {
                UserId = Guid.NewGuid(),
                Name = "John Instructor",
                Email = "instructor@example.com",
                Role = "Instructor",
                PasswordHash = "dummy_hash", // In real app, this would be properly hashed
                PasswordSalt = "dummy_salt"  // In real app, this would be properly salted
            };
            context.Users.Add(instructor);

            // Create sample students
            var students = new List<User>
            {
                new User
                {
                    UserId = Guid.NewGuid(),
                    Name = "Alice Student",
                    Email = "alice@example.com",
                    Role = "Student",
                    PasswordHash = "dummy_hash",
                    PasswordSalt = "dummy_salt"
                },
                new User
                {
                    UserId = Guid.NewGuid(),
                    Name = "Bob Student",
                    Email = "bob@example.com",
                    Role = "Student",
                    PasswordHash = "dummy_hash",
                    PasswordSalt = "dummy_salt"
                }
            };
            context.Users.AddRange(students);

            // Create sample course
            var course = new Course
            {
                CourseId = Guid.NewGuid(),
                Title = "Introduction to Programming",
                Description = "Learn the basics of programming",
                InstructorId = instructor.UserId
            };
            context.Courses.Add(course);

            // Create sample assessment
            var assessment = new Assessment
            {
                AssessmentId = Guid.NewGuid(),
                CourseId = course.CourseId,
                Title = "Basic Programming Concepts",
                MaxScore = 3,
                Questions = new List<Question>
                {
                    new Question
                    {
                        QuestionId = Guid.NewGuid(),
                        QuestionText = "What is a variable?",
                        Options = new List<Option>
                        {
                            new Option
                            {
                                OptionId = Guid.NewGuid(),
                                Text = "A container for storing data values",
                                IsCorrect = true
                            },
                            new Option
                            {
                                OptionId = Guid.NewGuid(),
                                Text = "A type of loop",
                                IsCorrect = false
                            },
                            new Option
                            {
                                OptionId = Guid.NewGuid(),
                                Text = "A function name",
                                IsCorrect = false
                            }
                        }
                    },
                    new Question
                    {
                        QuestionId = Guid.NewGuid(),
                        QuestionText = "Which of these is a loop structure?",
                        Options = new List<Option>
                        {
                            new Option
                            {
                                OptionId = Guid.NewGuid(),
                                Text = "if-else",
                                IsCorrect = false
                            },
                            new Option
                            {
                                OptionId = Guid.NewGuid(),
                                Text = "for",
                                IsCorrect = true
                            },
                            new Option
                            {
                                OptionId = Guid.NewGuid(),
                                Text = "switch",
                                IsCorrect = false
                            }
                        }
                    },
                    new Question
                    {
                        QuestionId = Guid.NewGuid(),
                        QuestionText = "What is the purpose of a function?",
                        Options = new List<Option>
                        {
                            new Option
                            {
                                OptionId = Guid.NewGuid(),
                                Text = "To store data",
                                IsCorrect = false
                            },
                            new Option
                            {
                                OptionId = Guid.NewGuid(),
                                Text = "To perform a specific task",
                                IsCorrect = true
                            },
                            new Option
                            {
                                OptionId = Guid.NewGuid(),
                                Text = "To declare variables",
                                IsCorrect = false
                            }
                        }
                    }
                }
            };
            context.Assessments.Add(assessment);

            // Create sample results
            var results = new List<Result>();
            foreach (var student in students)
            {
                var result = new Result
                {
                    ResultId = Guid.NewGuid(),
                    AssessmentId = assessment.AssessmentId,
                    UserId = student.UserId,
                    Score = new Random().Next(0, 4), // Random score between 0 and 3
                    AttemptDate = DateTime.UtcNow.AddDays(-new Random().Next(0, 7)), // Random date within last week
                    StudentAnswers = new List<StudentAnswer>()
                };

                // Add some random answers
                foreach (var question in assessment.Questions)
                {
                    var randomOption = question.Options.OrderBy(x => Guid.NewGuid()).First();
                    result.StudentAnswers.Add(new StudentAnswer
                    {
                        AnswerId = Guid.NewGuid(),
                        ResultId = result.ResultId,
                        QuestionId = question.QuestionId,
                        SelectedOptionId = randomOption.OptionId
                    });
                }

                results.Add(result);
            }
            context.Results.AddRange(results);

            await context.SaveChangesAsync();
        }
    }
} 