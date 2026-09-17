namespace Catore.Backend.Modules.Streak.Public;

public record StreakSummaryDto(
    int CurrentStreakCount,
    DateOnly? LastLoggedDate,
    bool StreakIsFrozen,
    int StreakFreezeCount,
    int WipeFreezeCount
);

// PRD 5.1: tanggal tertua/paling mendesak diberi label "Last day" (deadline hari ini juga),
// tanggal lain yg masih ada sisa waktu diberi label "X day(s) left" (bukan "missed" — kesannya
// gawat padahal masih ada waktu). DaysLeft dihitung di sini (bukan FE) krn butuh "now" server-side
// yg konsisten sama origin perhitungan Deadline (UTC).
public record MissingDateDto(
    DateOnly Date,
    DateTime Deadline,
    bool IsLastDay,
    int DaysLeft
);
