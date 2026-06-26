using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Service;
using IntelliCampus.Service_Abstraction;
using IntelliCampus.Shared.Dtos.Allocation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Moq;

namespace IntelliCampus.UnitTests.Services;

public class SpecializationAllocationHostedServiceTests
{
    [Fact]
    public void Constructor_DoesNotThrow()
    {
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        var loggerMock = new Mock<ILogger<SpecializationAllocationHostedService>>();

        var sut = new SpecializationAllocationHostedService(scopeFactoryMock.Object, loggerMock.Object);

        sut.Should().NotBeNull();
    }

    [Fact]
    public async Task StartAsync_DoesNotThrow()
    {
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        var loggerMock = new Mock<ILogger<SpecializationAllocationHostedService>>();
        var sut = new SpecializationAllocationHostedService(scopeFactoryMock.Object, loggerMock.Object);

        await sut.Invoking(s => s.StartAsync(CancellationToken.None)).Should().NotThrowAsync();
    }

    [Fact]
    public async Task StopAsync_DoesNotThrow()
    {
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        var loggerMock = new Mock<ILogger<SpecializationAllocationHostedService>>();
        var sut = new SpecializationAllocationHostedService(scopeFactoryMock.Object, loggerMock.Object);

        await sut.Invoking(s => s.StartAsync(CancellationToken.None)).Should().NotThrowAsync();
        await sut.Invoking(s => s.StopAsync(CancellationToken.None)).Should().NotThrowAsync();
    }

    [Fact]
    public async Task ExecuteAsync_StartStop_DoesNotThrow()
    {
        var scopeFactoryMock = new Mock<IServiceScopeFactory>();
        var loggerMock = new Mock<ILogger<SpecializationAllocationHostedService>>();

        var sut = new SpecializationAllocationHostedService(scopeFactoryMock.Object, loggerMock.Object);

        using var cts = new CancellationTokenSource();
        await sut.StartAsync(cts.Token);
        cts.Cancel();
        await sut.StopAsync(CancellationToken.None);

        sut.Should().NotBeNull();
    }
}
