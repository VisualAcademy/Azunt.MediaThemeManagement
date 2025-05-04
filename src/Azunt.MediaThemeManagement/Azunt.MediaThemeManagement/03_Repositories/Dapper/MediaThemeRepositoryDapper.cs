using Dapper;
using Azunt.Models.Common;
using Microsoft.Data.SqlClient;
using Microsoft.Extensions.Logging;

namespace Azunt.MediaThemeManagement;

public class MediaThemeRepositoryDapper : IMediaThemeRepository
{
    private readonly string _connectionString;
    private readonly ILogger<MediaThemeRepositoryDapper> _logger;

    public MediaThemeRepositoryDapper(string connectionString, ILoggerFactory loggerFactory)
    {
        _connectionString = connectionString;
        _logger = loggerFactory.CreateLogger<MediaThemeRepositoryDapper>();
    }

    private SqlConnection GetConnection() => new(_connectionString);

    public async Task<MediaTheme> AddAsync(MediaTheme model)
    {
        const string sql = @"
            INSERT INTO MediaThemes (Active, Created, CreatedBy, Name, IsDeleted)
            OUTPUT INSERTED.Id
            VALUES (@Active, @Created, @CreatedBy, @Name, 0)";

        model.Created = DateTimeOffset.UtcNow;

        using var conn = GetConnection();
        model.Id = await conn.ExecuteScalarAsync<long>(sql, model);
        return model;
    }

    public async Task<IEnumerable<MediaTheme>> GetAllAsync()
    {
        const string sql = @"
            SELECT Id, Active, Created, CreatedBy, Name 
            FROM MediaThemes 
            WHERE IsDeleted = 0 
            ORDER BY Id DESC";

        using var conn = GetConnection();
        return await conn.QueryAsync<MediaTheme>(sql);
    }

    public async Task<MediaTheme> GetByIdAsync(long id)
    {
        const string sql = @"
            SELECT Id, Active, Created, CreatedBy, Name 
            FROM MediaThemes 
            WHERE Id = @Id AND IsDeleted = 0";

        using var conn = GetConnection();
        return await conn.QuerySingleOrDefaultAsync<MediaTheme>(sql, new { Id = id }) ?? new MediaTheme();
    }

    public async Task<bool> UpdateAsync(MediaTheme model)
    {
        const string sql = @"
            UPDATE MediaThemes SET
                Active = @Active,
                Name = @Name
            WHERE Id = @Id AND IsDeleted = 0";

        using var conn = GetConnection();
        var affected = await conn.ExecuteAsync(sql, model);
        return affected > 0;
    }

    public async Task<bool> DeleteAsync(long id)
    {
        const string sql = @"
            UPDATE MediaThemes SET IsDeleted = 1 
            WHERE Id = @Id AND IsDeleted = 0";

        using var conn = GetConnection();
        var affected = await conn.ExecuteAsync(sql, new { Id = id });
        return affected > 0;
    }

    public async Task<ArticleSet<MediaTheme, int>> GetAllAsync<TParentIdentifier>(
        int pageIndex, int pageSize, string searchField, string searchQuery, string sortOrder, TParentIdentifier parentIdentifier)
    {
        var all = await GetAllAsync();
        var filtered = string.IsNullOrWhiteSpace(searchQuery)
            ? all
            : all.Where(m => m.Name != null && m.Name.Contains(searchQuery)).ToList();

        var paged = filtered
            .Skip(pageIndex * pageSize)
            .Take(pageSize)
            .ToList();

        return new ArticleSet<MediaTheme, int>(paged, filtered.Count());
    }

    public async Task<ArticleSet<MediaTheme, long>> GetAllAsync<TParentIdentifier>(Dul.Articles.FilterOptions<TParentIdentifier> options)
    {
        var all = await GetAllAsync();
        var filtered = all
            .Where(m => string.IsNullOrWhiteSpace(options.SearchQuery)
                     || (m.Name != null && m.Name.Contains(options.SearchQuery)))
            .ToList();

        var paged = filtered
            .Skip(options.PageIndex * options.PageSize)
            .Take(options.PageSize)
            .ToList();

        return new ArticleSet<MediaTheme, long>(paged, filtered.Count);
    }

    public async Task<bool> MoveUpAsync(long id)
    {
        const string getCurrent = "SELECT Id, DisplayOrder FROM MediaThemes WHERE Id = @Id AND IsDeleted = 0";
        const string getUpper = @"
        SELECT TOP 1 Id, DisplayOrder 
        FROM MediaThemes 
        WHERE DisplayOrder < @DisplayOrder AND IsDeleted = 0 
        ORDER BY DisplayOrder DESC";

        using var conn = GetConnection();
        await conn.OpenAsync();

        var current = await conn.QuerySingleOrDefaultAsync<(long Id, int DisplayOrder)>(getCurrent, new { Id = id });
        if (current.Id == 0) return false;

        var upper = await conn.QuerySingleOrDefaultAsync<(long Id, int DisplayOrder)>(getUpper, new { DisplayOrder = current.DisplayOrder });
        if (upper.Id == 0) return false;

        using var tx = conn.BeginTransaction();

        const string update = "UPDATE MediaThemes SET DisplayOrder = @DisplayOrder WHERE Id = @Id";

        await conn.ExecuteAsync(update, new { DisplayOrder = upper.DisplayOrder, Id = current.Id }, tx);
        await conn.ExecuteAsync(update, new { DisplayOrder = current.DisplayOrder, Id = upper.Id }, tx);

        tx.Commit();
        return true;
    }

    public async Task<bool> MoveDownAsync(long id)
    {
        const string getCurrent = "SELECT Id, DisplayOrder FROM MediaThemes WHERE Id = @Id AND IsDeleted = 0";
        const string getLower = @"
        SELECT TOP 1 Id, DisplayOrder 
        FROM MediaThemes 
        WHERE DisplayOrder > @DisplayOrder AND IsDeleted = 0 
        ORDER BY DisplayOrder ASC";

        using var conn = GetConnection();
        await conn.OpenAsync();

        var current = await conn.QuerySingleOrDefaultAsync<(long Id, int DisplayOrder)>(getCurrent, new { Id = id });
        if (current.Id == 0) return false;

        var lower = await conn.QuerySingleOrDefaultAsync<(long Id, int DisplayOrder)>(getLower, new { DisplayOrder = current.DisplayOrder });
        if (lower.Id == 0) return false;

        using var tx = conn.BeginTransaction();

        const string update = "UPDATE MediaThemes SET DisplayOrder = @DisplayOrder WHERE Id = @Id";

        await conn.ExecuteAsync(update, new { DisplayOrder = lower.DisplayOrder, Id = current.Id }, tx);
        await conn.ExecuteAsync(update, new { DisplayOrder = current.DisplayOrder, Id = lower.Id }, tx);

        tx.Commit();
        return true;
    }
}
