using System.Net;
using IntelliCampus.IntegrationTests.Helpers;
using IntelliCampus.Service_Abstraction;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace IntelliCampus.IntegrationTests.Tests;

public class DatabaseTimeoutTests
{
    [Fact]
    public async Task Test11_DB_Timeout_Registration_Returns_500()
    {
        var mock = new Mock<IRegistrationService>();
        mock.Setup(m => m.RegisterStudentInCourseAsync(
                It.IsAny<int>(), It.IsAny<IntelliCampus.Shared.Dtos.Registration.CourseRegistrationDto>()))
            .ThrowsAsync(new TaskCanceledException("DB command timeout"));

        using var factory = new CustomWebApplicationFactory(services =>
        {
            services.RemoveAll<IRegistrationService>();
            services.AddScoped<IRegistrationService>(_ => mock.Object);
        });

        var client = factory.CreateClientWithToken(JwtTokenHelper.StudentToken);
        var response = await client.PostAsync("/api/registration",
            TestHelper.ToJsonContent(new { courseId = 1, classId = 1 }));

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }

    [Fact]
    public async Task Test12_DB_Timeout_ExamSchedule_Returns_500()
    {
        var mock = new Mock<IExamScheduleService>();
        mock.Setup(m => m.GetByIdAsync(It.IsAny<int>()))
            .ThrowsAsync(new TaskCanceledException("DB command timeout"));

        using var factory = new CustomWebApplicationFactory(services =>
        {
            services.RemoveAll<IExamScheduleService>();
            services.AddScoped<IExamScheduleService>(_ => mock.Object);
        });

        var client = factory.CreateClientWithToken(JwtTokenHelper.StudentToken);
        var response = await client.GetAsync("/api/examschedule/1");

        Assert.Equal(HttpStatusCode.InternalServerError, response.StatusCode);
    }
}
