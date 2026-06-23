using System.Globalization;

namespace SmartPulseEpiasApp;

public class ConsoleTablePrinter
{
    private const int DateColumnWidth = 19;
    private const int NumericColumnWidth = 22;

    public void PrintTransactionSummaries(List<ContractSummary> summaries)
    {
        Console.WriteLine();
        Console.WriteLine("=== GİP İşlem Özeti ===");

        if (summaries.Count == 0)
        {
            Console.WriteLine("No transaction summary rows to display.");
            return;
        }

        var separator = new string('-', DateColumnWidth + (NumericColumnWidth * 3) + 3);
        Console.WriteLine(separator);
        Console.WriteLine(
            $"{PadRight("Tarih", DateColumnWidth)} {PadLeft("Toplam İşlem Tutarı", NumericColumnWidth)} {PadLeft("Toplam İşlem Miktarı", NumericColumnWidth)} {PadLeft("Ağırlıklı Ortalama Fiyat", NumericColumnWidth)}");
        Console.WriteLine(separator);

        foreach (var summary in summaries)
        {
            var dateText = summary.ContractDateTime.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
            Console.WriteLine(
                $"{PadRight(dateText, DateColumnWidth)} {FormatNumber(summary.TotalTransactionAmount)} {FormatNumber(summary.TotalTransactionQuantity)} {FormatNumber(summary.WeightedAveragePrice)}");
        }

        Console.WriteLine(separator);
        Console.WriteLine($"Rows: {summaries.Count}");
    }

    public void PrintKgup(List<KgupItem> items)
    {
        Console.WriteLine();
        Console.WriteLine("=== KGÜP ===");

        if (items.Count == 0)
        {
            Console.WriteLine("No KGÜP rows to display.");
            return;
        }

        const int dateWidth = 20;
        const int numWidth = 12;

        var separator = new string('-', dateWidth + (numWidth * 8) + 8);
        Console.WriteLine(separator);
        Console.WriteLine(
            $"{PadRight("Date", dateWidth)} {PadLeft("Toplam", numWidth)} {PadLeft("Doğalgaz", numWidth)} {PadLeft("Barajlı", numWidth)} {PadLeft("Akarsu", numWidth)} {PadLeft("Rüzgar", numWidth)} {PadLeft("Güneş", numWidth)} {PadLeft("İthal Kömür", numWidth)} {PadLeft("Linyit", numWidth)}");
        Console.WriteLine(separator);

        foreach (var item in items)
        {
            var dateText = FormatKgupDate(item.Date);
            Console.WriteLine(
                $"{PadRight(dateText, dateWidth)} {FormatNullableNumber(item.Toplam, numWidth)} {FormatNullableNumber(item.Dogalgaz, numWidth)} {FormatNullableNumber(item.Barajli, numWidth)} {FormatNullableNumber(item.Akarsu, numWidth)} {FormatNullableNumber(item.Ruzgar, numWidth)} {FormatNullableNumber(item.Gunes, numWidth)} {FormatNullableNumber(item.IthalKomur, numWidth)} {FormatNullableNumber(item.Linyit, numWidth)}");
        }

        Console.WriteLine(separator);
        Console.WriteLine($"Rows: {items.Count}");
    }

    public void PrintTradeValues(List<TradeValueItem> items)
    {
        Console.WriteLine();
        Console.WriteLine("=== GİP İşlem Hacmi ===");

        if (items.Count == 0)
        {
            Console.WriteLine("No GİP İşlem Hacmi rows to display.");
            return;
        }

        const int contractWidth = 24;
        const int typeWidth = 12;
        const int volumeWidth = 16;

        var separator = new string('-', contractWidth + typeWidth + volumeWidth + 2);
        Console.WriteLine(separator);
        Console.WriteLine(
            $"{PadRight("Contract", contractWidth)} {PadRight("Type", typeWidth)} {PadLeft("Trading Volume", volumeWidth)}");
        Console.WriteLine(separator);

        var sortedItems = items
            .OrderBy(item => item.KontratAdi ?? string.Empty, StringComparer.Ordinal)
            .ToList();

        foreach (var item in sortedItems)
        {
            var contract = item.KontratAdi ?? string.Empty;
            var type = item.KontratTuru ?? string.Empty;
            Console.WriteLine(
                $"{PadRight(contract, contractWidth)} {PadRight(type, typeWidth)} {FormatNullableNumber(item.TradingVolume, volumeWidth)}");
        }

        Console.WriteLine(separator);
        Console.WriteLine($"Rows: {items.Count}");
    }

    private static string PadRight(string value, int width) =>
        value.Length >= width ? value : value + new string(' ', width - value.Length);

    private static string PadLeft(string value, int width) =>
        value.Length >= width ? value : new string(' ', width - value.Length) + value;

    private static string FormatNumber(decimal value) =>
        PadLeft(value.ToString("0.0000", CultureInfo.InvariantCulture), NumericColumnWidth);

    private static string FormatKgupDate(string? date)
    {
        if (string.IsNullOrWhiteSpace(date))
        {
            return string.Empty;
        }

        if (DateTimeOffset.TryParse(date, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsed))
        {
            return parsed.ToString("dd/MM/yyyy HH:mm", CultureInfo.InvariantCulture);
        }

        return date;
    }

    private static string FormatNullableNumber(decimal? value, int width) =>
        PadLeft((value ?? 0m).ToString("0.0000", CultureInfo.InvariantCulture), width);
}
