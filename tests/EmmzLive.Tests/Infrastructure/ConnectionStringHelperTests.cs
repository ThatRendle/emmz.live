using EmmzLive.Infrastructure;

namespace EmmzLive.Tests.Infrastructure;

public sealed class ConnectionStringHelperTests
{
    [Theory]
    [InlineData("postgres://alice:s3cr3t@db.example.com:5432/mydb",
                "Host=db.example.com;Port=5432;Database=mydb;Username=alice;Password=s3cr3t")]
    [InlineData("postgresql://alice:s3cr3t@db.example.com:5432/mydb",
                "Host=db.example.com;Port=5432;Database=mydb;Username=alice;Password=s3cr3t")]
    [InlineData("postgres://user:pass@localhost:5432/emmz",
                "Host=localhost;Port=5432;Database=emmz;Username=user;Password=pass")]
    public void UrlForm_ConvertsToKeywordForm(string url, string expected)
    {
        var result = ConnectionStringHelper.ToNpgsqlConnectionString(url);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Host=db.example.com;Port=5432;Database=mydb;Username=alice;Password=s3cr3t")]
    [InlineData("Host=localhost;Database=emmz;Username=postgres;Password=postgres")]
    public void KeywordForm_PassesThroughUnchanged(string connectionString)
    {
        var result = ConnectionStringHelper.ToNpgsqlConnectionString(connectionString);
        Assert.Equal(connectionString, result);
    }

    [Fact]
    public void PasswordWithSpecialChars_DecodesCorrectly()
    {
        // Password containing '@' and '/' encoded as %40 and %2F
        const string url = "postgres://user:p%40ss%2Fword@db.example.com:5432/mydb";
        var result = ConnectionStringHelper.ToNpgsqlConnectionString(url);
        Assert.Contains("Password=p@ss/word", result);
    }

    [Fact]
    public void DefaultPort_WhenPortAbsentFromUrl()
    {
        // Some Railway URLs omit the port for the default 5432
        const string url = "postgres://user:pass@db.example.com/mydb";
        var result = ConnectionStringHelper.ToNpgsqlConnectionString(url);
        Assert.Contains("Port=5432", result);
    }

    [Fact]
    public void EmptyInput_Throws()
    {
        Assert.Throws<ArgumentException>(() =>
            ConnectionStringHelper.ToNpgsqlConnectionString(string.Empty));
    }

    [Fact]
    public void UrlWithSslModeRequire_MapsToNpgsqlSslMode()
    {
        const string url = "postgres://u:p@host:5432/db?sslmode=require";
        var result = ConnectionStringHelper.ToNpgsqlConnectionString(url);
        Assert.Equal("Host=host;Port=5432;Database=db;Username=u;Password=p;SSL Mode=Require", result);
    }

    [Fact]
    public void PostgresqlScheme_WithSslMode_IsRecognisedAndConverted()
    {
        // postgresql:// scheme + no port (defaults to 5432) + sslmode query param.
        const string url = "postgresql://u:p@host/db?sslmode=verify-full";
        var result = ConnectionStringHelper.ToNpgsqlConnectionString(url);
        Assert.Contains("SSL Mode=VerifyFull", result);
        Assert.Contains("Port=5432", result);
    }

    [Fact]
    public void KeywordFormContainingEquals_PassesThroughUnchanged()
    {
        // A keyword-form string with '=' must not be mistaken for a URL.
        const string cs = "Host=db.example.com;Port=5432;Database=mydb;Username=alice;Password=s3cr3t";
        var result = ConnectionStringHelper.ToNpgsqlConnectionString(cs);
        Assert.Equal(cs, result);
    }
}
