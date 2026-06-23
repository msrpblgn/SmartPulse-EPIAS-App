namespace SmartPulseEpiasApp;

public class TransactionHistoryItem
{
    public long Id { get; set; }
    public string? Date { get; set; }
    public string? Hour { get; set; }
    public string? ContractName { get; set; }
    public decimal Price { get; set; }
    public decimal Quantity { get; set; }
}

public class TransactionHistoryJsonRoot
{
    public List<TransactionHistoryItem>? Items { get; set; }
    public object? Page { get; set; }
}

public class ContractSummary
{
    public string ContractName { get; set; } = string.Empty;
    public DateTime ContractDateTime { get; set; }
    public decimal TotalTransactionAmount { get; set; }
    public decimal TotalTransactionQuantity { get; set; }
    public decimal WeightedAveragePrice { get; set; }
}

public class KgupRequest
{
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
}

public class KgupItem
{
    public string? Date { get; set; }
    public decimal? Akarsu { get; set; }
    public decimal? Barajli { get; set; }
    public decimal? Biokutle { get; set; }
    public decimal? Diger { get; set; }
    public decimal? Dogalgaz { get; set; }
    public decimal? FuelOil { get; set; }
    public decimal? Gunes { get; set; }
    public decimal? IthalKomur { get; set; }
    public decimal? Jeotermal { get; set; }
    public decimal? Linyit { get; set; }
    public decimal? Nafta { get; set; }
    public decimal? Ruzgar { get; set; }
    public decimal? TasKomur { get; set; }
    public decimal? Toplam { get; set; }
}

public class KgupTotals
{
    public decimal? AkarsuTotal { get; set; }
    public decimal? BarajliTotal { get; set; }
    public decimal? BiokutleTotal { get; set; }
    public decimal? DigerTotal { get; set; }
    public decimal? DogalgazTotal { get; set; }
    public decimal? FuelOilTotal { get; set; }
    public decimal? GunesTotal { get; set; }
    public decimal? IthalKomurTotal { get; set; }
    public decimal? JeotermalTotal { get; set; }
    public decimal? LinyitTotal { get; set; }
    public decimal? NaftaTotal { get; set; }
    public decimal? RuzgarTotal { get; set; }
    public decimal? TasKomurTotal { get; set; }
    public decimal? ToplamTotal { get; set; }
}

public class KgupApiResponse
{
    public List<KgupItem> Items { get; set; } = new();
    public PageRequest? Page { get; set; }
    public KgupTotals? Totals { get; set; }
}

public class TradeValueRequest
{
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
}

public class TradeValueItem
{
    public string? KontratAdi { get; set; }
    public string? KontratTuru { get; set; }
    public decimal? TradingVolume { get; set; }
}

public class TradeValueStatistics
{
    public decimal? TradingVolumeTotal { get; set; }
}

public class TradeValueApiResponse
{
    public List<TradeValueItem> Items { get; set; } = new();
    public PageRequest? Page { get; set; }
    public TradeValueStatistics? Statistics { get; set; }
}

public class ApiResponse<T>
{
    public List<T> Items { get; set; } = new();
    public PageRequest? Page { get; set; }
}

public class PageRequest
{
    public int? Page { get; set; }
    public int? Size { get; set; }
    public int? Number { get; set; }
}

public class TransactionHistoryMinimalRequest
{
    public string StartDate { get; set; } = string.Empty;
    public string EndDate { get; set; } = string.Empty;
}

public class EpiasSettings
{
    public string BaseUrl { get; set; } = string.Empty;
    public string AuthUrl { get; set; } = string.Empty;
    public string Region { get; set; } = string.Empty;
}

public class EpiasCredentials
{
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
}

public class AppSettings
{
    public EpiasSettings Epias { get; set; } = new();
}
