using System.Text.Json;
using FluentAssertions;
using IntelliCampus.Domain.Entities;
using IntelliCampus.Service;
using IntelliCampus.Shared.Settings;
using Microsoft.Extensions.Options;
using Moq;
using WebPush;

namespace IntelliCampus.UnitTests.Services;

public class WebPushSenderTests
{
    private readonly Mock<IOptions<VapidSettings>> _vapidOptionsMock;
    private readonly VapidSettings _vapidSettings;

    public WebPushSenderTests()
    {
        _vapidSettings = new VapidSettings
        {
            Subject = "mailto:test@test.com",
            PublicKey = "BP4HXOqQx_FJhYRvTqI-RNyMxQSR7N5s_sJfKpA7xh8YhB6zjGS5qY6Q3mVjPJDJHCk5B8HkdGm3kXlQzGq1B3s",
            PrivateKey = "6YfhBzXqQx_FJhYRvTqI-RNyMxQSR7N5s_sJfKpA7xh8"
        };
        _vapidOptionsMock = new Mock<IOptions<VapidSettings>>();
        _vapidOptionsMock.Setup(o => o.Value).Returns(_vapidSettings);
    }

    [Fact]
    public async Task SendAsync_EmptySubscriptions_ReturnsZeroCounts()
    {
        var sut = new WebPushSender(_vapidOptionsMock.Object);
        var result = await sut.SendAsync([], "Test", "Body", null, null, null);

        result.SuccessCount.Should().Be(0);
        result.FailureCount.Should().Be(0);
        _vapidOptionsMock.VerifyGet(o => o.Value, Times.Once());
    }

    [Fact]
    public async Task SendAsync_SingleValidSubscription_ReturnsSuccessCountOne()
    {
        var sender = new TestableWebPushSender(_vapidOptionsMock.Object, null);
        var subs = new[]
        {
            new DeviceToken { Endpoint = "https://example.com/push", P256dh = "key1", Auth = "auth1" }
        };

        var result = await sender.SendAsync(subs, "Title", "Body", null, null, null);

        result.SuccessCount.Should().Be(1);
        result.FailureCount.Should().Be(0);
        result.InvalidTokens.Should().BeEmpty();
        _vapidOptionsMock.VerifyGet(o => o.Value, Times.Once());
    }

    [Fact]
    public async Task SendAsync_MultipleSubscriptions_ReportsCorrectCounts()
    {
        var exceptions = new Queue<Exception?>();
        exceptions.Enqueue(null);
        exceptions.Enqueue(new Exception("410 Gone"));

        var sender = new TestableWebPushSender(_vapidOptionsMock.Object, _ => exceptions.Dequeue());
        var subs = new[]
        {
            new DeviceToken { Endpoint = "https://example.com/valid", P256dh = "key1", Auth = "auth1" },
            new DeviceToken { Endpoint = "https://example.com/gone", P256dh = "key2", Auth = "auth2" }
        };

        var result = await sender.SendAsync(subs, "Title", "Body", null, null, null);

        result.SuccessCount.Should().Be(1);
        result.FailureCount.Should().Be(1);
        result.InvalidTokens.Should().ContainSingle()
            .Which.Endpoint.Should().Be("https://example.com/gone");
        _vapidOptionsMock.VerifyGet(o => o.Value, Times.Once());
    }

    [Fact]
    public async Task SendAsync_Exception410Gone_AddsToInvalidTokens()
    {
        var sender = new TestableWebPushSender(_vapidOptionsMock.Object, _ => new Exception("410 Gone"));
        var sub = new DeviceToken { Endpoint = "https://example.com/gone", P256dh = "key", Auth = "auth" };

        var result = await sender.SendAsync([sub], "Title", "Body", null, null, null);

        result.SuccessCount.Should().Be(0);
        result.FailureCount.Should().Be(1);
        result.InvalidTokens.Should().ContainSingle().Which.Endpoint.Should().Be(sub.Endpoint);
        _vapidOptionsMock.VerifyGet(o => o.Value, Times.Once());
    }

    [Fact]
    public async Task SendAsync_Exception404NotFound_AddsToInvalidTokens()
    {
        var sender = new TestableWebPushSender(_vapidOptionsMock.Object, _ => new Exception("Not Found"));
        var sub = new DeviceToken { Endpoint = "https://example.com/notfound", P256dh = "key", Auth = "auth" };

        var result = await sender.SendAsync([sub], "Title", "Body", null, null, null);

        result.InvalidTokens.Should().ContainSingle().Which.Endpoint.Should().Be(sub.Endpoint);
        _vapidOptionsMock.VerifyGet(o => o.Value, Times.Once());
    }

    [Fact]
    public async Task SendAsync_ExceptionContainsGone_AddsToInvalidTokens()
    {
        var sender = new TestableWebPushSender(_vapidOptionsMock.Object, _ => new Exception("Resource is Gone"));
        var sub = new DeviceToken { Endpoint = "https://example.com/gone", P256dh = "key", Auth = "auth" };

        var result = await sender.SendAsync([sub], "Title", "Body", null, null, null);

        result.InvalidTokens.Should().ContainSingle().Which.Endpoint.Should().Be(sub.Endpoint);
        _vapidOptionsMock.VerifyGet(o => o.Value, Times.Once());
    }

    [Fact]
    public async Task SendAsync_NonRecoverableException_IncrementsFailureCountOnly()
    {
        var sender = new TestableWebPushSender(_vapidOptionsMock.Object, _ => new Exception("Network error"));
        var sub = new DeviceToken { Endpoint = "https://example.com/error", P256dh = "key", Auth = "auth" };

        var result = await sender.SendAsync([sub], "Title", "Body", null, null, null);

        result.SuccessCount.Should().Be(0);
        result.FailureCount.Should().Be(1);
        result.InvalidTokens.Should().BeEmpty();
        _vapidOptionsMock.VerifyGet(o => o.Value, Times.Once());
    }

    [Fact]
    public async Task SendAsync_NullTitle_UsesDefaultIntelliCampus()
    {
        string? capturedPayload = null;
        var sender = new TestableWebPushSender(_vapidOptionsMock.Object, (payload, _, _) => capturedPayload = payload);

        var sub = new DeviceToken { Endpoint = "https://example.com/push", P256dh = "key", Auth = "auth" };
        await sender.SendAsync([sub], null, "Body", "/click", null, null);

        var json = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(capturedPayload);
        json!["title"].GetString().Should().Be("IntelliCampus");
        _vapidOptionsMock.VerifyGet(o => o.Value, Times.Once());
    }

    [Fact]
    public async Task SendAsync_NullClickUrl_UsesDefaultForwardSlash()
    {
        string? capturedPayload = null;
        var sender = new TestableWebPushSender(_vapidOptionsMock.Object, (payload, _, _) => capturedPayload = payload);

        var sub = new DeviceToken { Endpoint = "https://example.com/push", P256dh = "key", Auth = "auth" };
        await sender.SendAsync([sub], "Title", "Body", null, null, null);

        var json = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(capturedPayload);
        json!["clickUrl"].GetString().Should().Be("/");
        _vapidOptionsMock.VerifyGet(o => o.Value, Times.Once());
    }

    [Fact]
    public async Task SendAsync_NullOptionalFields_SerializesAsNull()
    {
        string? capturedPayload = null;
        var sender = new TestableWebPushSender(_vapidOptionsMock.Object, (payload, _, _) => capturedPayload = payload);

        var sub = new DeviceToken { Endpoint = "https://example.com/push", P256dh = "key", Auth = "auth" };
        await sender.SendAsync([sub], "Title", "Body", "/click", null, null);

        var json = JsonSerializer.Deserialize<Dictionary<string, JsonElement>>(capturedPayload);
        json!.ContainsKey("imageUrl").Should().BeTrue();
        json["imageUrl"].ValueKind.Should().Be(JsonValueKind.Null);
        json.ContainsKey("notificationId").Should().BeTrue();
        json["notificationId"].ValueKind.Should().Be(JsonValueKind.Null);
        _vapidOptionsMock.VerifyGet(o => o.Value, Times.Once());
    }

    [Fact]
    public async Task SendAsync_VapidDetailsPassedToClient()
    {
        VapidDetails? capturedVapid = null;
        var sender = new TestableWebPushSender(_vapidOptionsMock.Object, (_, _, v) => capturedVapid = v);

        var sub = new DeviceToken { Endpoint = "https://example.com/push", P256dh = "key", Auth = "auth" };
        await sender.SendAsync([sub], "Title", "Body", null, null, null);

        capturedVapid!.Subject.Should().Be("mailto:test@test.com");
        _vapidOptionsMock.VerifyGet(o => o.Value, Times.Once());
    }

    [Fact]
    public async Task SendAsync_AllSubscriptionFieldsPassedToPushSubscription()
    {
        PushSubscription? capturedSub = null;
        var sender = new TestableWebPushSender(_vapidOptionsMock.Object, (_, s, _) => capturedSub = s);

        var sub = new DeviceToken
        {
            Endpoint = "https://example.com/custom",
            P256dh = "customP256dh",
            Auth = "customAuth"
        };
        await sender.SendAsync([sub], "Title", "Body", null, null, null);

        capturedSub!.Endpoint.Should().Be("https://example.com/custom");
        capturedSub.P256DH.Should().Be("customP256dh");
        capturedSub.Auth.Should().Be("customAuth");
        _vapidOptionsMock.VerifyGet(o => o.Value, Times.Once());
    }

    [Fact]
    public async Task SendAsync_EmptyVapidSubject_UsesEmptyString()
    {
        var vapidSettings = new VapidSettings
        {
            Subject = string.Empty,
            PublicKey = _vapidSettings.PublicKey,
            PrivateKey = _vapidSettings.PrivateKey
        };
        var vapidMock = new Mock<IOptions<VapidSettings>>();
        vapidMock.Setup(o => o.Value).Returns(vapidSettings);

        VapidDetails? capturedVapid = null;
        var sender = new TestableWebPushSender(vapidMock.Object, (_, _, v) => capturedVapid = v);

        var sub = new DeviceToken { Endpoint = "https://example.com/push", P256dh = "key", Auth = "auth" };
        await sender.SendAsync([sub], "Title", "Body", null, null, null);

        capturedVapid!.Subject.Should().Be(string.Empty);
        _vapidOptionsMock.VerifyGet(o => o.Value, Times.Never());
        vapidMock.VerifyGet(o => o.Value, Times.Once());
    }

    [Fact]
    public async Task SendAsync_AllSubscriptionsFailWith410_AddsAllToInvalidTokens()
    {
        var sender = new TestableWebPushSender(_vapidOptionsMock.Object, _ => new Exception("410 Gone"));
        var subs = new[]
        {
            new DeviceToken { Endpoint = "https://example.com/a", P256dh = "key1", Auth = "auth1" },
            new DeviceToken { Endpoint = "https://example.com/b", P256dh = "key2", Auth = "auth2" },
            new DeviceToken { Endpoint = "https://example.com/c", P256dh = "key3", Auth = "auth3" }
        };

        var result = await sender.SendAsync(subs, "Title", "Body", null, null, null);

        result.SuccessCount.Should().Be(0);
        result.FailureCount.Should().Be(3);
        result.InvalidTokens.Should().HaveCount(3);
        _vapidOptionsMock.VerifyGet(o => o.Value, Times.Once());
    }

    [Fact]
    public async Task SendAsync_ExceptionWith404Code_AddsToInvalidTokens()
    {
        var sender = new TestableWebPushSender(_vapidOptionsMock.Object, _ => new Exception("HTTP 404 Not Found"));
        var sub = new DeviceToken { Endpoint = "https://example.com/404", P256dh = "key", Auth = "auth" };

        var result = await sender.SendAsync([sub], "Title", "Body", null, null, null);

        result.InvalidTokens.Should().ContainSingle().Which.Endpoint.Should().Be(sub.Endpoint);
        _vapidOptionsMock.VerifyGet(o => o.Value, Times.Once());
    }

    [Fact]
    public async Task SendAsync_MixedFailures_Only410And404AddedToInvalid()
    {
        var exceptions = new Queue<Exception?>();
        exceptions.Enqueue(new Exception("410 Gone"));
        exceptions.Enqueue(new Exception("Network timeout"));
        exceptions.Enqueue(new Exception("Not Found"));
        exceptions.Enqueue(null);

        var sender = new TestableWebPushSender(_vapidOptionsMock.Object, _ => exceptions.Dequeue());
        var subs = new[]
        {
            new DeviceToken { Endpoint = "https://example.com/1", P256dh = "k1", Auth = "a1" },
            new DeviceToken { Endpoint = "https://example.com/2", P256dh = "k2", Auth = "a2" },
            new DeviceToken { Endpoint = "https://example.com/3", P256dh = "k3", Auth = "a3" },
            new DeviceToken { Endpoint = "https://example.com/4", P256dh = "k4", Auth = "a4" }
        };

        var result = await sender.SendAsync(subs, "Title", "Body", null, null, null);

        result.SuccessCount.Should().Be(1);
        result.FailureCount.Should().Be(3);
        result.InvalidTokens.Should().HaveCount(2);
        _vapidOptionsMock.VerifyGet(o => o.Value, Times.Once());
    }

    [Fact]
    public async Task SendAsync_EmptyVapidPublicKey_DoesNotThrow()
    {
        var vapidSettings = new VapidSettings
        {
            Subject = "mailto:test@test.com",
            PublicKey = string.Empty,
            PrivateKey = _vapidSettings.PrivateKey
        };
        var vapidMock = new Mock<IOptions<VapidSettings>>();
        vapidMock.Setup(o => o.Value).Returns(vapidSettings);

        var sender = new TestableWebPushSender(vapidMock.Object, null);
        var sub = new DeviceToken { Endpoint = "https://example.com/push", P256dh = "key", Auth = "auth" };

        var result = await sender.SendAsync([sub], "Title", "Body", null, null, null);

        result.SuccessCount.Should().Be(1);
        _vapidOptionsMock.VerifyGet(o => o.Value, Times.Never());
        vapidMock.VerifyGet(o => o.Value, Times.Once());
    }

    [Fact]
    public async Task SendAsync_EmptyVapidPrivateKey_DoesNotThrow()
    {
        var vapidSettings = new VapidSettings
        {
            Subject = "mailto:test@test.com",
            PublicKey = _vapidSettings.PublicKey,
            PrivateKey = string.Empty
        };
        var vapidMock = new Mock<IOptions<VapidSettings>>();
        vapidMock.Setup(o => o.Value).Returns(vapidSettings);

        var sender = new TestableWebPushSender(vapidMock.Object, null);
        var sub = new DeviceToken { Endpoint = "https://example.com/push", P256dh = "key", Auth = "auth" };

        var result = await sender.SendAsync([sub], "Title", "Body", null, null, null);

        result.SuccessCount.Should().Be(1);
        _vapidOptionsMock.VerifyGet(o => o.Value, Times.Never());
        vapidMock.VerifyGet(o => o.Value, Times.Once());
    }

    private sealed class TestableWebPushSender : WebPushSender
    {
        private readonly Func<string?, Exception?>? _getException;
        private readonly Action<string?, PushSubscription, VapidDetails>? _capture;

        public TestableWebPushSender(IOptions<VapidSettings> settings, Func<string?, Exception?>? getException)
            : base(settings) => _getException = getException;

        public TestableWebPushSender(IOptions<VapidSettings> settings, Action<string?, PushSubscription, VapidDetails> capture)
            : base(settings) => _capture = capture;

        protected override Task SendOneAsync(WebPushClient client, PushSubscription subscription, string payload, VapidDetails vapidDetails)
        {
            _capture?.Invoke(payload, subscription, vapidDetails);

            var ex = _getException?.Invoke(payload);
            if (ex is not null)
                throw ex;

            return Task.CompletedTask;
        }
    }
}
