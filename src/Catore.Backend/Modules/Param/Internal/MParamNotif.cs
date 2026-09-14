namespace Catore.Backend.Modules.Param.Internal;

internal class MParamNotif
{
    public long NotifPk { get; set; }
    public string Key { get; set; } = string.Empty;
    public long? Type { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
    public bool IsActive { get; set; } = true;
    public DateTime CreatedOn { get; set; }
    public DateTime ModifiedOn { get; set; }
}
