using System.Data.Common;

using Backend.Data;
using Backend.Models.Entities;

using DotNet.Testcontainers.Builders;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Npgsql;
using Testcontainers.PostgreSql;
using Testcontainers.Xunit;
using Xunit.Sdk;

namespace DatabaseLayer.Tests;

public sealed class TestDatabaseFixture(IMessageSink messageSink)
    : DbContainerFixture<PostgreSqlBuilder, PostgreSqlContainer>(messageSink)
{
    protected override PostgreSqlBuilder Configure() =>
        new PostgreSqlBuilder("postgres:17")
            .WithEnvironment("POSTGRES_USER", PostgreSqlBuilder.DefaultUsername)
            .WithEnvironment("POSTGRES_PASSWORD", PostgreSqlBuilder.DefaultPassword)
            .WithEnvironment("POSTGRES_DB", "main")
            .WithPortBinding(PostgreSqlBuilder.PostgreSqlPort, assignRandomHostPort: true)
            .WithWaitStrategy(Wait.ForUnixContainer()
                .UntilInternalTcpPortIsAvailable(PostgreSqlBuilder.PostgreSqlPort)
                .UntilExternalTcpPortIsAvailable(PostgreSqlBuilder.PostgreSqlPort));

    protected override async ValueTask InitializeAsync()
    {
        await base.InitializeAsync();

        await using var context = CreateContext();

        await context.Database.MigrateAsync();

        context.AddRange([
            new OperationalRestriction {
                 StartDate = DateTime.UtcNow
             },
            new OperationalRestriction {
                 StartDate = DateTime.UtcNow
             }
        ]);
        await context.SaveChangesAsync();
    }

    public override DbProviderFactory DbProviderFactory => NpgsqlFactory.Instance;

    public AppDbContext CreateContext()
    {
        return new AppDbContext(
            new DbContextOptionsBuilder<AppDbContext>()
                .UseNpgsql(ConnectionString)
                .UseQueryTrackingBehavior(QueryTrackingBehavior.NoTracking)
                .ConfigureWarnings(w => w.Ignore(CoreEventId.ManyServiceProvidersCreatedWarning))
                .Options);
    }
}