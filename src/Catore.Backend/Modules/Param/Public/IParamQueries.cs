namespace Catore.Backend.Modules.Param.Public;

public interface IParamQueries
{
    // Resolve nama tampilan (mis. "Male", "Moderately active") -> paramPk, dipakai saat SIMPAN data
    // (request dari FE tetap string, service internal convert ke FK sebelum persist ke DB).
    Task<long?> ResolvePk(string paramType, string name);

    // Resolve paramPk -> nama tampilan, dipakai saat BACA data (DB simpan FK, response API tetap string).
    Task<string?> ResolveName(long? paramPk);
}
