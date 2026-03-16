using CountOrSell.Domain.Models;
using Microsoft.EntityFrameworkCore;

namespace CountOrSell.Data.Repositories;

public class UserExportFileRepository : IUserExportFileRepository
{
    private readonly AppDbContext _db;
    public UserExportFileRepository(AppDbContext db) => _db = db;

    public async Task<UserExportFile> CreateAsync(UserExportFile exportFile, CancellationToken ct = default)
    {
        _db.UserExportFiles.Add(exportFile);
        await _db.SaveChangesAsync(ct);
        return exportFile;
    }

    public Task<List<UserExportFile>> GetByUserIdAsync(Guid userId, CancellationToken ct = default) =>
        _db.UserExportFiles.Where(f => f.UserId == userId).OrderByDescending(f => f.CreatedAt).ToListAsync(ct);

    public Task<UserExportFile?> GetByIdAsync(Guid id, CancellationToken ct = default) =>
        _db.UserExportFiles.FirstOrDefaultAsync(f => f.Id == id, ct);

    public async Task DeleteAsync(Guid id, CancellationToken ct = default)
    {
        var file = await _db.UserExportFiles.FindAsync(new object[] { id }, ct);
        if (file != null)
        {
            _db.UserExportFiles.Remove(file);
            await _db.SaveChangesAsync(ct);
        }
    }
}
