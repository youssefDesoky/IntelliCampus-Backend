using System.Text;
using System.Text.Json;
using IntelliCampus.Domain.Interfaces;
using IntelliCampus.Presistence.Data.Contexts;
using IntelliCampus.Service_Abstraction;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Data.SqlClient;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Moq;

namespace IntelliCampus.IntegrationTests.Helpers;

public class CustomWebApplicationFactory : WebApplicationFactory<Program>
{
    public Mock<IRoutingClientService> RoutingClientMock { get; } = new();
    public Mock<ITurnstileVerifier> TurnstileMock { get; } = new();
    public Mock<IEmailSender> EmailSenderMock { get; } = new();
    public Mock<IPushSender> PushSenderMock { get; } = new();

    private readonly Action<IServiceCollection>? _additionalConfigure;
    private readonly string _connectionString;

    public CustomWebApplicationFactory(Action<IServiceCollection>? additionalConfigure = null)
    {
        _additionalConfigure = additionalConfigure;
        var dbName = $"IntelliCampus_Test_{Guid.NewGuid():N}";
        _connectionString = $@"Server=(localdb)\MSSQLLocalDB;Database={dbName};Trusted_Connection=True;MultipleActiveResultSets=True;TrustServerCertificate=True;";
        TurnstileMock.Setup(m => m.VerifyAsync(It.IsAny<string?>()))
            .ReturnsAsync(true);
    }

    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.ConfigureTestServices(services =>
        {
            // Remove pooled DbContext and replace with a fresh LocalDB database
            var descriptors = services.Where(d =>
                d.ServiceType == typeof(DbContextOptions<IntelliCampusDbContext>) ||
                d.ServiceType == typeof(IntelliCampusDbContext)).ToList();
            foreach (var d in descriptors)
                services.Remove(d);

            services.AddDbContextPool<IntelliCampusDbContext>(options =>
                options.UseSqlServer(_connectionString));

            // IDataSeed - no-op stub to skip seeding
            services.RemoveAll<IDataSeed>();
            services.AddScoped<IDataSeed>(_ => Mock.Of<IDataSeed>());

            // IRoutingClientService - mockable
            services.RemoveAll<IRoutingClientService>();
            services.AddScoped<IRoutingClientService>(_ => RoutingClientMock.Object);

            // ITurnstileVerifier - stub returns true
            services.RemoveAll<ITurnstileVerifier>();
            services.AddScoped<ITurnstileVerifier>(_ => TurnstileMock.Object);

            // IEmailSender - no-op stub
            services.RemoveAll<IEmailSender>();
            services.AddScoped<IEmailSender>(_ => EmailSenderMock.Object);

            // IPushSender - no-op stub (singleton)
            services.RemoveAll<IPushSender>();
            services.AddSingleton<IPushSender>(_ => PushSenderMock.Object);

            // Additional per-test configuration
            _additionalConfigure?.Invoke(services);
        });

        builder.UseEnvironment("Development");
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            // Cleanup: drop the test database
            try
            {
                using var conn = new SqlConnection(@"Server=(localdb)\MSSQLLocalDB;Trusted_Connection=True;TrustServerCertificate=True;");
                conn.Open();
                using var cmd = conn.CreateCommand();
                // Extract DB name from connection string
                var parts = _connectionString.Split(';');
                var dbPart = parts.FirstOrDefault(p => p.StartsWith("Database=", StringComparison.OrdinalIgnoreCase));
                if (dbPart != null)
                {
                    var dbName = dbPart.Split('=')[1];
                    cmd.CommandText = $"IF EXISTS (SELECT name FROM sys.databases WHERE name = @db) BEGIN ALTER DATABASE [{dbName}] SET SINGLE_USER WITH ROLLBACK IMMEDIATE; DROP DATABASE [{dbName}]; END";
                    cmd.Parameters.AddWithValue("@db", dbName);
                    cmd.ExecuteNonQuery();
                }
            }
            catch { /* best-effort cleanup */ }
        }
        base.Dispose(disposing);
    }

    public HttpClient CreateClientWithToken(string token)
    {
        var client = CreateClient();
        client.DefaultRequestHeaders.Add("Cookie", $"token={token}");
        return client;
    }
}
