namespace Catore.Backend.Modules.Consumption.Internal;

internal class DailyRecord
{
    public Guid DailyRecordPk { get; set; }
    public Guid UserId { get; set; }
    public DateOnly RecordDate { get; set; }
    public string DeficitCategory { get; set; } = string.Empty;
    public bool PaToday { get; set; }
    public decimal EffectiveTdee { get; set; }
    public decimal EffectiveLimit { get; set; }
    public string CreatedVia { get; set; } = string.Empty;
    public bool IsFrozen { get; set; }
    public DateTime ModifiedOn { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? IsDeletedOn { get; set; }
}
