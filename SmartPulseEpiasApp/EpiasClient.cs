using System.Net.Http.Headers;
using System.Text;
using System.Text.Encodings.Web;
using System.Text.Json;

namespace SmartPulseEpiasApp;

public class EpiasClient
{
    private static readonly HttpClient HttpClient = new();
    private static readonly JsonSerializerOptions JsonApiOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase
    };
    private static readonly JsonSerializerOptions RelaxedJsonApiOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping
    };

    private readonly EpiasSettings _settings;
    private readonly EpiasCredentials? _credentials;

    private string? _cachedTgt;
    private DateTime? _cachedTgtExpiresAtUtc;

    public EpiasClient(EpiasSettings settings, EpiasCredentials? credentials = null)
    {
        _settings = settings;
        _credentials = credentials;
    }

    public EpiasSettings Settings => _settings;
    public EpiasCredentials? Credentials => _credentials;

    public async Task<string?> GetTgtAsync()
    {
        if (!string.IsNullOrWhiteSpace(_cachedTgt) &&
            _cachedTgtExpiresAtUtc.HasValue &&
            _cachedTgtExpiresAtUtc.Value > DateTime.UtcNow)
        {
            Console.WriteLine("Using cached TGT.");
            return _cachedTgt;
        }

        if (_credentials == null)
        {
            Console.WriteLine("EPİAŞ credentials are missing. Set EPIAS_USERNAME and EPIAS_PASSWORD.");
            return null;
        }

        if (string.IsNullOrWhiteSpace(_settings.AuthUrl))
        {
            Console.WriteLine("EPİAŞ AuthUrl is not configured.");
            return null;
        }

        try
        {
            using var request = new HttpRequestMessage(HttpMethod.Post, _settings.AuthUrl);
            request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("text/plain"));
            request.Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["username"] = _credentials.Username,
                ["password"] = _credentials.Password
            });
            request.Content.Headers.ContentType = new MediaTypeHeaderValue("application/x-www-form-urlencoded");

            using var response = await HttpClient.SendAsync(request);

            if (!response.IsSuccessStatusCode)
            {
                Console.WriteLine($"EPİAŞ TGT login failed. HTTP status: {(int)response.StatusCode} {response.ReasonPhrase}");
                return null;
            }

            var tgt = await ExtractTgtFromResponseAsync(response);
            if (string.IsNullOrWhiteSpace(tgt))
            {
                Console.WriteLine("EPİAŞ TGT login failed. No TGT was returned in the response.");
                return null;
            }

            _cachedTgt = tgt.Trim();
            _cachedTgtExpiresAtUtc = DateTime.UtcNow.AddHours(2);

            Console.WriteLine("TGT received successfully.");
            Console.WriteLine($"TGT cached until {_cachedTgtExpiresAtUtc:yyyy-MM-dd HH:mm:ss} UTC.");

            return _cachedTgt;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"EPİAŞ TGT login failed due to a network error: {ex.Message}");
            return null;
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("EPİAŞ TGT login failed because the request timed out.");
            return null;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"EPİAŞ TGT login failed: {ex.Message}");
            return null;
        }
    }

    public async Task<List<TransactionHistoryItem>> GetTransactionHistoryAsync()
    {
        var tgt = await GetTgtAsync();
        if (string.IsNullOrWhiteSpace(tgt))
        {
            Console.WriteLine("Cannot fetch transaction-history because TGT could not be obtained.");
            return new List<TransactionHistoryItem>();
        }

        if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
        {
            Console.WriteLine("EPİAŞ BaseUrl is not configured.");
            return new List<TransactionHistoryItem>();
        }

        var (startDate, endDate) = GetMostRecentCompleteTurkeyDateRange();
        Console.WriteLine("Fetching live transaction-history data from EPİAŞ...");
        Console.WriteLine($"Date range: {startDate} to {endDate}");

        try
        {
            var result = await SendTransactionHistoryPowerShellEquivalentRequestAsync(tgt, startDate, endDate);

            if (!result.IsSuccessStatusCode)
            {
                Console.WriteLine($"Transaction-history request failed. HTTP status: {result.StatusCode} {result.ReasonPhrase}");
                PrintSafeResponsePreview(result.ResponseText);
                return new List<TransactionHistoryItem>();
            }

            return result.Items;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"Transaction-history request failed due to a network error: {ex.Message}");
            return new List<TransactionHistoryItem>();
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("Transaction-history request failed because the request timed out.");
            return new List<TransactionHistoryItem>();
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"Transaction-history response could not be parsed: {ex.Message}");
            return new List<TransactionHistoryItem>();
        }
        catch (Exception ex)
        {
            Console.WriteLine($"Transaction-history request failed: {ex.Message}");
            return new List<TransactionHistoryItem>();
        }
    }

    private async Task<TransactionHistoryRequestResult> SendTransactionHistoryPowerShellEquivalentRequestAsync(
        string tgt,
        string startDate,
        string endDate)
    {
        var url = $"{_settings.BaseUrl.TrimEnd('/')}/v1/markets/idm/data/transaction-history";
        var body = JsonSerializer.Serialize(
            new TransactionHistoryMinimalRequest
            {
                StartDate = startDate,
                EndDate = endDate
            },
            RelaxedJsonApiOptions);

        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.TryAddWithoutValidation("TGT", tgt);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var content = new ByteArrayContent(Encoding.UTF8.GetBytes(body));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        request.Content = content;

        using var response = await HttpClient.SendAsync(request);
        var responseText = await response.Content.ReadAsStringAsync();

        List<TransactionHistoryItem> items = new();

        if (response.IsSuccessStatusCode)
        {
            try
            {
                var apiResponse = JsonSerializer.Deserialize<ApiResponse<TransactionHistoryItem>>(responseText, JsonApiOptions);
                if (apiResponse?.Items != null)
                {
                    items = apiResponse.Items;
                }
            }
            catch (JsonException)
            {
                items = new List<TransactionHistoryItem>();
            }
        }

        return new TransactionHistoryRequestResult
        {
            StatusCode = (int)response.StatusCode,
            ReasonPhrase = response.ReasonPhrase ?? string.Empty,
            IsSuccessStatusCode = response.IsSuccessStatusCode,
            ResponseText = responseText,
            Items = items
        };
    }

    private static (string StartDate, string EndDate) GetMostRecentCompleteTurkeyDateRange()
    {
        var turkeyOffset = TimeSpan.FromHours(3);
        var turkeyNow = DateTimeOffset.UtcNow.ToOffset(turkeyOffset);
        var turkeyTodayStart = new DateTimeOffset(
            turkeyNow.Year,
            turkeyNow.Month,
            turkeyNow.Day,
            0,
            0,
            0,
            turkeyOffset);
        var turkeyYesterdayStart = turkeyTodayStart.AddDays(-1);
        const string format = "yyyy-MM-dd'T'HH:mm:sszzz";

        return (turkeyYesterdayStart.ToString(format), turkeyTodayStart.ToString(format));
    }

    private static void PrintSafeResponsePreview(string? responseText)
    {
        if (string.IsNullOrWhiteSpace(responseText))
        {
            return;
        }

        var preview = responseText.Trim();
        if (preview.Length > 500)
        {
            preview = preview[..500] + "...";
        }

        Console.WriteLine($"Response preview: {preview}");
    }

    private static async Task<string?> ExtractTgtFromResponseAsync(HttpResponseMessage response)
    {
        var body = (await response.Content.ReadAsStringAsync()).Trim();
        if (!string.IsNullOrWhiteSpace(body))
        {
            return body;
        }

        if (response.Headers.Location != null)
        {
            return ExtractTgtFromLocation(response.Headers.Location);
        }

        return null;
    }

    private static string? ExtractTgtFromLocation(Uri location)
    {
        var segments = location.AbsolutePath
            .Split('/', StringSplitOptions.RemoveEmptyEntries);

        if (segments.Length == 0)
        {
            return null;
        }

        var lastSegment = segments[^1];
        return string.IsNullOrWhiteSpace(lastSegment) ? null : lastSegment;
    }

    private sealed class TransactionHistoryRequestResult
    {
        public int StatusCode { get; init; }
        public string ReasonPhrase { get; init; } = string.Empty;
        public bool IsSuccessStatusCode { get; init; }
        public string? ResponseText { get; init; }
        public List<TransactionHistoryItem> Items { get; init; } = new();
    }

    public async Task<KgupApiResponse> GetKgupAsync()
    {
        var emptyResponse = new KgupApiResponse();

        var tgt = await GetTgtAsync();
        if (string.IsNullOrWhiteSpace(tgt))
        {
            Console.WriteLine("Cannot fetch KGÜP because TGT could not be obtained.");
            return emptyResponse;
        }

        if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
        {
            Console.WriteLine("EPİAŞ BaseUrl is not configured.");
            return emptyResponse;
        }

        var url = $"{_settings.BaseUrl.TrimEnd('/')}/v1/generation/data/dpp";
        var (startDate, endDate) = GetMostRecentCompleteTurkeyDateRange();
        var region = string.IsNullOrWhiteSpace(_settings.Region) ? "TR1" : _settings.Region;

        Console.WriteLine("Fetching KGÜP data from EPİAŞ...");
        Console.WriteLine($"Endpoint URL: {url}");
        Console.WriteLine($"Date range: {startDate} to {endDate}");
        Console.WriteLine($"Region: {region}");

        try
        {
            var body = JsonSerializer.Serialize(
                new KgupRequest
                {
                    StartDate = startDate,
                    EndDate = endDate,
                    Region = region
                },
                RelaxedJsonApiOptions);

            var postResult = await SendPowerShellEquivalentJsonPostAsync(tgt, url, body);

            if (!postResult.IsSuccessStatusCode)
            {
                Console.WriteLine($"KGÜP request failed. HTTP status: {postResult.StatusCode} {postResult.ReasonPhrase}");
                PrintSafeResponsePreview(postResult.ResponseText);
                return emptyResponse;
            }

            var apiResponse = JsonSerializer.Deserialize<KgupApiResponse>(postResult.ResponseText, JsonApiOptions)
                ?? emptyResponse;

            Console.WriteLine($"Loaded {apiResponse.Items.Count} KGÜP rows.");

            if (apiResponse.Totals?.ToplamTotal is decimal toplamTotal)
            {
                Console.WriteLine($"Toplam total: {toplamTotal:0.0000}");
            }

            return apiResponse;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"KGÜP request failed due to a network error: {ex.Message}");
            return emptyResponse;
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("KGÜP request failed because the request timed out.");
            return emptyResponse;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"KGÜP response could not be parsed: {ex.Message}");
            return emptyResponse;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"KGÜP request failed: {ex.Message}");
            return emptyResponse;
        }
    }

    private async Task<PowerShellEquivalentPostResult> SendPowerShellEquivalentJsonPostAsync(
        string tgt,
        string url,
        string body)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, url);
        request.Headers.TryAddWithoutValidation("TGT", tgt);
        request.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));

        var content = new ByteArrayContent(Encoding.UTF8.GetBytes(body));
        content.Headers.ContentType = new MediaTypeHeaderValue("application/json");
        request.Content = content;

        using var response = await HttpClient.SendAsync(request);
        var responseText = await response.Content.ReadAsStringAsync();

        return new PowerShellEquivalentPostResult
        {
            StatusCode = (int)response.StatusCode,
            ReasonPhrase = response.ReasonPhrase ?? string.Empty,
            IsSuccessStatusCode = response.IsSuccessStatusCode,
            ContentType = response.Content.Headers.ContentType?.ToString(),
            ResponseText = responseText,
            SentContentType = content.Headers.ContentType?.ToString()
        };
    }

    private sealed class PowerShellEquivalentPostResult
    {
        public int StatusCode { get; init; }
        public string ReasonPhrase { get; init; } = string.Empty;
        public bool IsSuccessStatusCode { get; init; }
        public string? ContentType { get; init; }
        public string ResponseText { get; init; } = string.Empty;
        public string? SentContentType { get; init; }
    }

    public async Task<TradeValueApiResponse> GetGipIslemHacmiAsync()
    {
        var emptyResponse = new TradeValueApiResponse();

        var tgt = await GetTgtAsync();
        if (string.IsNullOrWhiteSpace(tgt))
        {
            Console.WriteLine("Cannot fetch GİP İşlem Hacmi because TGT could not be obtained.");
            return emptyResponse;
        }

        if (string.IsNullOrWhiteSpace(_settings.BaseUrl))
        {
            Console.WriteLine("EPİAŞ BaseUrl is not configured.");
            return emptyResponse;
        }

        var url = $"{_settings.BaseUrl.TrimEnd('/')}/v1/markets/idm/data/trade-value";
        var (startDate, endDate) = GetMostRecentCompleteTurkeyDateRange();

        Console.WriteLine("Fetching GİP İşlem Hacmi data from EPİAŞ...");
        Console.WriteLine($"Endpoint URL: {url}");
        Console.WriteLine($"Date range: {startDate} to {endDate}");

        try
        {
            var body = JsonSerializer.Serialize(
                new TradeValueRequest
                {
                    StartDate = startDate,
                    EndDate = endDate
                },
                RelaxedJsonApiOptions);

            Console.WriteLine($"Request body: {body}");

            var postResult = await SendPowerShellEquivalentJsonPostAsync(tgt, url, body);

            if (!postResult.IsSuccessStatusCode)
            {
                Console.WriteLine($"GİP İşlem Hacmi request failed. HTTP status: {postResult.StatusCode} {postResult.ReasonPhrase}");
                PrintSafeResponsePreview(postResult.ResponseText);
                return emptyResponse;
            }

            var apiResponse = JsonSerializer.Deserialize<TradeValueApiResponse>(postResult.ResponseText, JsonApiOptions)
                ?? emptyResponse;

            Console.WriteLine($"Loaded {apiResponse.Items.Count} GİP İşlem Hacmi rows.");

            if (apiResponse.Statistics?.TradingVolumeTotal is decimal tradingVolumeTotal)
            {
                Console.WriteLine($"Trading volume total: {tradingVolumeTotal:0.0000}");
            }

            return apiResponse;
        }
        catch (HttpRequestException ex)
        {
            Console.WriteLine($"GİP İşlem Hacmi request failed due to a network error: {ex.Message}");
            return emptyResponse;
        }
        catch (TaskCanceledException)
        {
            Console.WriteLine("GİP İşlem Hacmi request failed because the request timed out.");
            return emptyResponse;
        }
        catch (JsonException ex)
        {
            Console.WriteLine($"GİP İşlem Hacmi response could not be parsed: {ex.Message}");
            return emptyResponse;
        }
        catch (Exception ex)
        {
            Console.WriteLine($"GİP İşlem Hacmi request failed: {ex.Message}");
            return emptyResponse;
        }
    }
}
