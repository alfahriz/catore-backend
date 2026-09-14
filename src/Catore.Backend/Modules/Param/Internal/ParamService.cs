using Catore.Backend.Modules.Param.Public;

namespace Catore.Backend.Modules.Param.Internal;

internal class ParamService : IParamQueries
{
    private readonly ParamRepository _repository;

    public ParamService(ParamRepository repository)
    {
        _repository = repository;
    }

    public async Task<long?> ResolvePk(string paramType, string name)
    {
        var param = await _repository.GetByTypeAndName(paramType, name);
        return param?.ParamPk;
    }

    public async Task<string?> ResolveName(long? paramPk)
    {
        if (paramPk is null) return null;
        var param = await _repository.GetByPk(paramPk.Value);
        return param?.Name;
    }
}
