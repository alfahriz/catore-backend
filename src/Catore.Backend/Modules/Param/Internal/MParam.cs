namespace Catore.Backend.Modules.Param.Internal;

internal class MParam
{
    public long ParamPk { get; set; }
    public string ParamType { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public int Value { get; set; }
    public DateTime CreatedOn { get; set; }
    public long? CreatedBy { get; set; }
}
