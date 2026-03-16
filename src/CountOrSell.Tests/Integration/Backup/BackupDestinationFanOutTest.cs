using CountOrSell.Api.Services;
using CountOrSell.Data;
using CountOrSell.Domain.Models;
using CountOrSell.Domain.Models.Enums;
using CountOrSell.Domain.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using Moq;
using Xunit;

namespace CountOrSell.Tests.Integration.Backup;

public class BackupDestinationFanOutTest
{
    private static AppDbContext CreateDb()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase($"FanOutTest_{Guid.NewGuid()}")
            .Options;
        return new AppDbContext(options);
    }

    private static IConfiguration CreateConfig(string instanceName = "test")
    {
        return new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["INSTANCE_NAME"] = instanceName
            })
            .Build();
    }

    [Fact]
    public async Task TakeBackup_WritesToBothDestinations_WhenBothSucceed()
    {
        using var db = CreateDb();

        // Seed two active destination configs
        var config1 = new BackupDestinationConfig
        {
            Id = Guid.NewGuid(),
            DestinationType = "local",
            Label = "Dest1",
            ConfigurationJson = "{}",
            IsActive = true
        };
        var config2 = new BackupDestinationConfig
        {
            Id = Guid.NewGuid(),
            DestinationType = "local",
            Label = "Dest2",
            ConfigurationJson = "{}",
            IsActive = true
        };
        db.BackupDestinationConfigs.AddRange(config1, config2);
        await db.SaveChangesAsync();

        var dest1Written = false;
        var dest2Written = false;

        var mockDest1 = new Mock<IBackupDestination>();
        mockDest1.Setup(d => d.DestinationType).Returns("local");
        mockDest1.Setup(d => d.Label).Returns("Dest1");
        mockDest1.Setup(d => d.WriteAsync(
            It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Callback(() => dest1Written = true)
            .Returns(Task.CompletedTask);
        mockDest1.Setup(d => d.DeleteAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockDest2 = new Mock<IBackupDestination>();
        mockDest2.Setup(d => d.DestinationType).Returns("local");
        mockDest2.Setup(d => d.Label).Returns("Dest2");
        mockDest2.Setup(d => d.WriteAsync(
            It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Callback(() => dest2Written = true)
            .Returns(Task.CompletedTask);
        mockDest2.Setup(d => d.DeleteAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var factory = new Mock<IBackupDestinationFactory>();
        factory.Setup(f => f.Create(It.Is<BackupDestinationConfig>(c => c.Label == "Dest1")))
            .Returns(mockDest1.Object);
        factory.Setup(f => f.Create(It.Is<BackupDestinationConfig>(c => c.Label == "Dest2")))
            .Returns(mockDest2.Object);

        var mockRunner = new Mock<IProcessRunner>();
        mockRunner.Setup(r => r.RunAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, string>?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync("-- dump sql");

        var mockSchemaVersion = new Mock<ISchemaVersionService>();
        mockSchemaVersion.Setup(s => s.GetCurrentSchemaVersionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        var mockNotify = new Mock<IAdminNotificationService>();

        var svc = new BackupService(
            db,
            mockSchemaVersion.Object,
            factory.Object,
            mockNotify.Object,
            CreateConfig(),
            mockRunner.Object,
            new Mock<ILogger<BackupService>>().Object);

        var record = await svc.TakeBackupAsync(BackupType.Scheduled, CancellationToken.None);

        Assert.True(dest1Written, "Destination 1 should have been written");
        Assert.True(dest2Written, "Destination 2 should have been written");
        Assert.Equal(2, record.Destinations.Count);
        Assert.All(record.Destinations, d => Assert.True(d.Success));
    }

    [Fact]
    public async Task TakeBackup_SucceedsOnFirstDest_WhenSecondFails_AndNotifiesAdmin()
    {
        using var db = CreateDb();

        var config1 = new BackupDestinationConfig
        {
            Id = Guid.NewGuid(),
            DestinationType = "local",
            Label = "GoodDest",
            ConfigurationJson = "{}",
            IsActive = true
        };
        var config2 = new BackupDestinationConfig
        {
            Id = Guid.NewGuid(),
            DestinationType = "azure-blob",
            Label = "BadDest",
            ConfigurationJson = "{}",
            IsActive = true
        };
        db.BackupDestinationConfigs.AddRange(config1, config2);
        await db.SaveChangesAsync();

        var goodDestWritten = false;

        var mockGoodDest = new Mock<IBackupDestination>();
        mockGoodDest.Setup(d => d.DestinationType).Returns("local");
        mockGoodDest.Setup(d => d.Label).Returns("GoodDest");
        mockGoodDest.Setup(d => d.WriteAsync(
            It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .Callback(() => goodDestWritten = true)
            .Returns(Task.CompletedTask);
        mockGoodDest.Setup(d => d.DeleteAsync(
            It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Returns(Task.CompletedTask);

        var mockBadDest = new Mock<IBackupDestination>();
        mockBadDest.Setup(d => d.DestinationType).Returns("azure-blob");
        mockBadDest.Setup(d => d.Label).Returns("BadDest");
        mockBadDest.Setup(d => d.WriteAsync(
            It.IsAny<string>(), It.IsAny<Stream>(), It.IsAny<CancellationToken>()))
            .ThrowsAsync(new InvalidOperationException("Azure connection failed"));

        var factory = new Mock<IBackupDestinationFactory>();
        factory.Setup(f => f.Create(It.Is<BackupDestinationConfig>(c => c.Label == "GoodDest")))
            .Returns(mockGoodDest.Object);
        factory.Setup(f => f.Create(It.Is<BackupDestinationConfig>(c => c.Label == "BadDest")))
            .Returns(mockBadDest.Object);

        var mockRunner = new Mock<IProcessRunner>();
        mockRunner.Setup(r => r.RunAsync(
            It.IsAny<string>(),
            It.IsAny<string>(),
            It.IsAny<Dictionary<string, string>?>(),
            It.IsAny<string?>(),
            It.IsAny<CancellationToken>()))
            .ReturnsAsync("-- dump sql");

        var mockSchemaVersion = new Mock<ISchemaVersionService>();
        mockSchemaVersion.Setup(s => s.GetCurrentSchemaVersionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(1);

        string? notifiedMessage = null;
        var mockNotify = new Mock<IAdminNotificationService>();
        mockNotify.Setup(n => n.NotifyAsync(
            It.IsAny<string>(), It.IsAny<string>(), It.IsAny<CancellationToken>()))
            .Callback<string, string, CancellationToken>((msg, cat, _) => notifiedMessage = msg)
            .Returns(Task.CompletedTask);

        var svc = new BackupService(
            db,
            mockSchemaVersion.Object,
            factory.Object,
            mockNotify.Object,
            CreateConfig(),
            mockRunner.Object,
            new Mock<ILogger<BackupService>>().Object);

        var record = await svc.TakeBackupAsync(BackupType.Scheduled, CancellationToken.None);

        Assert.True(goodDestWritten, "Good destination should have been written");
        Assert.Equal(2, record.Destinations.Count);

        var goodResult = record.Destinations.Single(d => d.DestinationLabel == "GoodDest");
        var badResult = record.Destinations.Single(d => d.DestinationLabel == "BadDest");

        Assert.True(goodResult.Success);
        Assert.False(badResult.Success);
        Assert.NotNull(badResult.ErrorMessage);

        // Admin should have been notified about the failed destination
        Assert.NotNull(notifiedMessage);
        Assert.Contains("BadDest", notifiedMessage);
    }
}
