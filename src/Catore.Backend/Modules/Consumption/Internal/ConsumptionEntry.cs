namespace Catore.Backend.Modules.Consumption.Internal;

// Bank makanan/minuman GLOBAL/shared, dedup by name (trim+lowercase) + calories exact
// match (lihat unique index di AppDbContext). Beda dari TConsumption (transaksi tiap
// kali user catat) -- 1 baris di sini bisa dirujuk banyak TConsumption dari user manapun.
internal class MConsumption
{
    public long ConsumptionPk { get; set; }
    public string Name { get; set; } = string.Empty;
    public int Calories { get; set; }
    public DateTime CreatedOn { get; set; }
    public long CreatedBy { get; set; }
}

// Transaksi pencatatan konsumsi -- 1 baris tiap kali user simpan 1 item dalam batch.
// TIDAK simpan Calories sendiri (selalu JOIN ke MConsumption via ConsumptionId).
internal class TConsumption
{
    public long EntryPk { get; set; }
    public long UserId { get; set; }
    public long ConsumptionId { get; set; }
    public long? MealType { get; set; }
    public DateTime EntryTimestamp { get; set; }
    public DateTime CreatedOn { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? IsDeletedOn { get; set; }
}
