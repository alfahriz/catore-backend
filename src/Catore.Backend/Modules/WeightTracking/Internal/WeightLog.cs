namespace Catore.Backend.Modules.WeightTracking.Internal;

internal class WeightLog
{
    public Guid WeightLogPk { get; set; }
    public Guid UserId { get; set; }
    public decimal WeightValue { get; set; }
    public DateTime LoggedAt { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? ModifiedOn { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? IsDeletedOn { get; set; }
}
