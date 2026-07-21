namespace Catore.Backend.Modules.SharedKernel;

public static class WeekPartitionHelper
{
    // W1 mulai tanggal 1 tiap bulan, W1-W5 tergantung jumlah hari.
    public static int GetWeekNumberInMonth(DateOnly date)
    {
        return ((date.Day - 1) / 7) + 1;
    }
}
