using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;

namespace Azunt.MediaThemeManagement;

public class MediaThemeAppDbContextFactory
{
    private readonly IConfiguration? _configuration;

    public MediaThemeAppDbContextFactory() { }

    public MediaThemeAppDbContextFactory(IConfiguration configuration)
    {
        _configuration = configuration;
    }

    public MediaThemeAppDbContext CreateDbContext(string connectionString)
    {
        var options = new DbContextOptionsBuilder<MediaThemeAppDbContext>()
            .UseSqlServer(connectionString)
            .Options;

        return new MediaThemeAppDbContext(options);
    }

    public MediaThemeAppDbContext CreateDbContext(DbContextOptions<MediaThemeAppDbContext> options)
    {
        ArgumentNullException.ThrowIfNull(options);
        return new MediaThemeAppDbContext(options);
    }

    public MediaThemeAppDbContext CreateDbContext()
    {
        if (_configuration == null)
        {
            throw new InvalidOperationException("Configuration is not provided.");
        }

        var defaultConnection = _configuration.GetConnectionString("DefaultConnection");

        if (string.IsNullOrWhiteSpace(defaultConnection))
        {
            throw new InvalidOperationException("DefaultConnection is not configured properly.");
        }

        return CreateDbContext(defaultConnection);
    }
}