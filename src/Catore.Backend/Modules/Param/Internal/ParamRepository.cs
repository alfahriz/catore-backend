using Microsoft.EntityFrameworkCore;
using Catore.Backend.Infrastructure;

namespace Catore.Backend.Modules.Param.Internal;

internal class ParamRepository
{
    private readonly AppDbContext _db;

    public ParamRepository(AppDbContext db)
    {
        _db = db;
    }

    public async Task<MParam?> GetByPk(long paramPk)
    {
        return await _db.Set<MParam>().FirstOrDefaultAsync(p => p.ParamPk == paramPk);
    }

    public async Task<MParam?> GetByTypeAndName(string paramType, string name)
    {
        return await _db.Set<MParam>().FirstOrDefaultAsync(p => p.ParamType == paramType && p.Name == name);
    }

    public async Task<List<MParam>> GetByType(string paramType)
    {
        return await _db.Set<MParam>().Where(p => p.ParamType == paramType).ToListAsync();
    }
}
