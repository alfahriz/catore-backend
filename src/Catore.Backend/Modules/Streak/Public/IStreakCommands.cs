namespace Catore.Backend.Modules.Streak.Public;

public interface IStreakCommands
{
    Task FreezeStreak(long userId);
    Task RecordDailyLog(long userId, DateOnly date);
    // Reset streak ke 0 + catat alasan — dipakai wipe-ganti-mode (WipeReason="Manual") dan
    // bisa dipakai ulang untuk wipe rutin nanti kalau StreakService.EvaluateWipeCheck mau
    // direfactor pakai method ini juga (saat ini masih inline langsung di sana).
    Task ResetStreak(long userId, string wipeReason);
}
