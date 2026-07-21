namespace Catore.Backend.Modules.Consumption.Internal;

internal class ConsumptionEntry
{
    public Guid ConsumptionEntryPk { get; set; }
    public Guid UserId { get; set; }
    public DateOnly EntryDate { get; set; }
    public string MealType { get; set; } = string.Empty;
    public string FoodName { get; set; } = string.Empty;
    public int Calories { get; set; }
    public DateTime EntryTimestamp { get; set; }
    public DateTime CreatedAt { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? IsDeletedOn { get; set; }
}
