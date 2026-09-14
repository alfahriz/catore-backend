namespace Catore.Backend.Modules.Consumption.Internal;

internal class TDailyRecord
{
    public long DailyRecordPk { get; set; }
    public long UserId { get; set; }
    public DateOnly RecordDate { get; set; }
    public long? DeficitCategory { get; set; }
    public bool PaToday { get; set; }
    public decimal EffectiveTdee { get; set; }
    public decimal EffectiveLimit { get; set; }
    public int ActualCalories { get; set; }
    public long? CreatedVia { get; set; }
    public bool IsFrozen { get; set; }
    public DateTime ModifiedOn { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? IsDeletedOn { get; set; }
}
