using System;

public static class CurrencyFormatter
{
    // Mảng các hậu tố tiền tệ thường dùng trong game Tycoon
    private static readonly string[] Suffixes = { "", "K", "M", "B", "T", "aa", "ab", "ac", "ad", "ae", "af", "ag", "ah", "ai", "aj" };

    public static string FormatMoney(double value)
    {
        // Nếu dưới 1000 thì hiển thị số bình thường
        if (value < 1000d)
        {
            return Math.Floor(value).ToString("F0");
        }

        int suffixIndex = 0;
        double displayValue = value;

        // Chia cho 1000 liên tục cho đến khi số nhỏ hơn 1000 hoặc hết mảng hậu tố
        while (displayValue >= 1000d && suffixIndex < Suffixes.Length - 1)
        {
            displayValue /= 1000d;
            suffixIndex++;
        }

        // Hiển thị 2 chữ số thập phân (vd: 1.52K, 3.00M)
        return displayValue.ToString("F2") + Suffixes[suffixIndex];
    }
}
