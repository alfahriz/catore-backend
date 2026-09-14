namespace Catore.Backend.Modules.WeightTracking.Internal;

// Kolom checkpointDate (rename dari loggedAt lama) -- LOGIC service tetap "1 entry per
// hari via tanggal" (sama seperti sebelumnya), BELUM implementasi upsert mingguan
// (checkpoint Jumat, carry-forward) yang direncanakan di schema-curation. Itu logic
// baru yang sengaja ditunda ke sesi terpisah — jangan diasumsikan sudah jalan.
internal class TWeightLog
{
    public long WeightLogPk { get; set; }
    public long UserId { get; set; }
    public DateOnly CheckpointDate { get; set; }
    public decimal Weight { get; set; }
    public DateTime CreatedOn { get; set; }
    public DateTime? ModifiedOn { get; set; }
    public bool IsDeleted { get; set; }
    public DateTime? IsDeletedOn { get; set; }
}
