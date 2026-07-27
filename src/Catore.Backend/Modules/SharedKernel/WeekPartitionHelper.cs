namespace Catore.Backend.Modules.SharedKernel;

public static class WeekPartitionHelper
{
    // W1 mulai tanggal 1 tiap bulan, W1-W5 tergantung jumlah hari.
    public static int GetWeekNumberInMonth(DateOnly date)
    {
        return ((date.Day - 1) / 7) + 1;
    }

    // Kebalikan dari GetWeekNumberInMonth: dari (tahun, bulan, W-ke berapa) -> rentang tanggal minggu itu.
    // W1 selalu tanggal 1-7 (boleh dipotong kalau bulan lebih pendek), lalu siklus 7 hari, dipotong di batas bulan.
    public static (DateOnly Start, DateOnly End) GetWeekDateRange(int year, int month, int weekNumber)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        var start = new DateOnly(year, month, 1).AddDays((weekNumber - 1) * 7);
        var end = start.AddDays(6);

        var monthEnd = new DateOnly(year, month, daysInMonth);
        if (end > monthEnd) end = monthEnd;

        return (start, end);
    }

    // Jumlah minggu (baris) dalam 1 bulan, ikut partisi yang sama (W1 mulai tanggal 1).
    public static int GetWeekCountInMonth(int year, int month)
    {
        var daysInMonth = DateTime.DaysInMonth(year, month);
        return (int)Math.Ceiling(daysInMonth / 7.0);
    }
}
