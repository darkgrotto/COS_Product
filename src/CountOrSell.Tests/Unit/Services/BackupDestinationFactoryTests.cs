using CountOrSell.Api.Services.Destinations;
using CountOrSell.Domain.Models;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace CountOrSell.Tests.Unit.Services;

public class BackupDestinationFactoryTests
{
    private static BackupDestinationFactory CreateFactory()
    {
        var config = new ConfigurationBuilder().Build();
        return new BackupDestinationFactory(config);
    }

    [Fact]
    public void Create_AzureBlob_Throws_When_ConnectionString_Missing()
    {
        var factory = CreateFactory();
        var config = new BackupDestinationConfig
        {
            Id = Guid.NewGuid(),
            DestinationType = "azure-blob",
            Label = "Primary",
            ConfigurationJson = "{}",
            IsActive = true
        };

        var ex = Assert.Throws<ArgumentException>(() => factory.Create(config));
        Assert.Contains("connectionString", ex.Message);
        Assert.Contains("Primary", ex.Message);
    }

    [Fact]
    public void Create_AzureBlob_Builds_With_Default_Container_When_Only_ConnectionString_Provided()
    {
        var factory = CreateFactory();
        var config = new BackupDestinationConfig
        {
            Id = Guid.NewGuid(),
            DestinationType = "azure-blob",
            Label = "Primary",
            // Azurite well-known dev connection string - exercises constructor parsing
            // without contacting a real endpoint.
            ConfigurationJson =
                "{\"connectionString\":\"UseDevelopmentStorage=true\"}",
            IsActive = true
        };

        var dest = factory.Create(config);

        Assert.IsType<AzureBlobBackupDestination>(dest);
        Assert.Equal("azure-blob", dest.DestinationType);
        Assert.Equal("Primary", dest.Label);
    }

    [Fact]
    public void Create_AzureBlob_Builds_With_Custom_Container()
    {
        var factory = CreateFactory();
        var config = new BackupDestinationConfig
        {
            Id = Guid.NewGuid(),
            DestinationType = "azure-blob",
            Label = "WithContainer",
            ConfigurationJson =
                "{\"connectionString\":\"UseDevelopmentStorage=true\",\"containerName\":\"custom-name\"}",
            IsActive = true
        };

        var dest = factory.Create(config);

        Assert.IsType<AzureBlobBackupDestination>(dest);
        Assert.Equal("WithContainer", dest.Label);
    }

    [Fact]
    public void Create_AwsS3_Throws_When_Bucket_Missing()
    {
        var factory = CreateFactory();
        var config = new BackupDestinationConfig
        {
            Id = Guid.NewGuid(),
            DestinationType = "aws-s3",
            Label = "S3Primary",
            ConfigurationJson = "{\"region\":\"us-east-1\"}",
            IsActive = true
        };

        var ex = Assert.Throws<ArgumentException>(() => factory.Create(config));
        Assert.Contains("bucket", ex.Message);
        Assert.Contains("S3Primary", ex.Message);
    }

    [Fact]
    public void Create_AwsS3_Throws_When_Region_Missing()
    {
        var factory = CreateFactory();
        var config = new BackupDestinationConfig
        {
            Id = Guid.NewGuid(),
            DestinationType = "aws-s3",
            Label = "S3Primary",
            ConfigurationJson = "{\"bucket\":\"my-bucket\"}",
            IsActive = true
        };

        var ex = Assert.Throws<ArgumentException>(() => factory.Create(config));
        Assert.Contains("region", ex.Message);
        Assert.Contains("S3Primary", ex.Message);
    }

    [Fact]
    public void Create_AwsS3_Builds_With_Required_Fields()
    {
        var factory = CreateFactory();
        var config = new BackupDestinationConfig
        {
            Id = Guid.NewGuid(),
            DestinationType = "aws-s3",
            Label = "S3Primary",
            ConfigurationJson =
                "{\"bucket\":\"my-bucket\",\"region\":\"us-east-1\"}",
            IsActive = true
        };

        var dest = factory.Create(config);

        Assert.IsType<AwsS3BackupDestination>(dest);
        Assert.Equal("aws-s3", dest.DestinationType);
        Assert.Equal("S3Primary", dest.Label);
    }

    [Fact]
    public void Create_AwsS3_Throws_When_AccessKey_Without_SecretKey()
    {
        var factory = CreateFactory();
        var config = new BackupDestinationConfig
        {
            Id = Guid.NewGuid(),
            DestinationType = "aws-s3",
            Label = "S3Primary",
            ConfigurationJson =
                "{\"bucket\":\"my-bucket\",\"region\":\"us-east-1\",\"accessKey\":\"AKIA...\"}",
            IsActive = true
        };

        var ex = Assert.Throws<ArgumentException>(() => factory.Create(config));
        Assert.Contains("secretKey", ex.Message);
    }

    [Fact]
    public void Create_AwsS3_Throws_When_SecretKey_Without_AccessKey()
    {
        // Mirror of the access-without-secret check: a partial-paste of just the
        // secret must fail loudly rather than silently falling through to the
        // SDK default credential chain.
        var factory = CreateFactory();
        var config = new BackupDestinationConfig
        {
            Id = Guid.NewGuid(),
            DestinationType = "aws-s3",
            Label = "S3Primary",
            ConfigurationJson =
                "{\"bucket\":\"my-bucket\",\"region\":\"us-east-1\",\"secretKey\":\"shhh...\"}",
            IsActive = true
        };

        var ex = Assert.Throws<ArgumentException>(() => factory.Create(config));
        Assert.Contains("accessKey", ex.Message);
    }

    [Fact]
    public void Create_GcpStorage_Throws_When_Bucket_Missing()
    {
        var factory = CreateFactory();
        var config = new BackupDestinationConfig
        {
            Id = Guid.NewGuid(),
            DestinationType = "gcp-storage",
            Label = "GcsPrimary",
            ConfigurationJson = "{\"projectId\":\"my-project\"}",
            IsActive = true
        };

        var ex = Assert.Throws<ArgumentException>(() => factory.Create(config));
        Assert.Contains("bucket", ex.Message);
        Assert.Contains("GcsPrimary", ex.Message);
    }

    [Fact]
    public void Create_GcpStorage_Throws_When_ProjectId_Missing()
    {
        var factory = CreateFactory();
        var config = new BackupDestinationConfig
        {
            Id = Guid.NewGuid(),
            DestinationType = "gcp-storage",
            Label = "GcsPrimary",
            ConfigurationJson = "{\"bucket\":\"my-bucket\"}",
            IsActive = true
        };

        var ex = Assert.Throws<ArgumentException>(() => factory.Create(config));
        Assert.Contains("projectId", ex.Message);
        Assert.Contains("GcsPrimary", ex.Message);
    }

    [Fact]
    public void Create_GcpStorage_Builds_With_Endpoint_Override()
    {
        var factory = CreateFactory();
        var config = new BackupDestinationConfig
        {
            Id = Guid.NewGuid(),
            DestinationType = "gcp-storage",
            Label = "GcsPrimary",
            // endpoint set so the builder uses UnauthenticatedAccess and does not require
            // Application Default Credentials at construction time
            ConfigurationJson =
                "{\"bucket\":\"my-bucket\",\"projectId\":\"my-project\","
                + "\"endpoint\":\"http://localhost:4443\"}",
            IsActive = true
        };

        var dest = factory.Create(config);

        Assert.IsType<GcpStorageBackupDestination>(dest);
        Assert.Equal("gcp-storage", dest.DestinationType);
        Assert.Equal("GcsPrimary", dest.Label);
    }
}
