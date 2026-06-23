using System.Text.Json;
using SmartPulseEpiasApp;

var appSettings = LoadSettings(out var settingsWarning);
if (!string.IsNullOrWhiteSpace(settingsWarning))
{
    Console.WriteLine(settingsWarning);
}

var credentials = LoadEpiasCredentials();
PrintStartupStatus(appSettings.Epias, credentials);

var client = new EpiasClient(appSettings.Epias, credentials);
var summaryService = new TransactionSummaryService();
var printer = new ConsoleTablePrinter();

while (true)
{
    PrintMenu();
    Console.Write("Please select: ");
    var choice = Console.ReadLine()?.Trim();

    switch (choice)
    {
        case "1":
            await ShowLiveTransactionSummaryAsync(client, summaryService, printer);
            break;
        case "2":
            await ShowKgupAsync(client, printer);
            break;
        case "3":
            await ShowGipIslemHacmiAsync(client, printer);
            break;
        case "4":
            await ShowAllAsync(client, summaryService, printer);
            break;
        case "5":
            await TestEpiasTgtLoginAsync(client);
            break;
        case "6":
            ShowLocalTransactionSummary(summaryService, printer);
            break;
        case "0":
            Console.WriteLine("Exiting.");
            return;
        default:
            Console.WriteLine("Invalid option. Please choose 0, 1, 2, 3, 4, 5, or 6.");
            break;
    }

    Console.WriteLine();
}

static void PrintMenu()
{
    Console.WriteLine();
    Console.WriteLine("SmartPulse EPİAŞ App");
    Console.WriteLine();
    Console.WriteLine("1 - GİP İşlem Özeti");
    Console.WriteLine("2 - KGÜP");
    Console.WriteLine("3 - GİP İşlem Hacmi");
    Console.WriteLine("4 - All");
    Console.WriteLine("5 - Test EPİAŞ TGT Login");
    Console.WriteLine("6 - GİP İşlem Özeti (Local JSON Test)");
    Console.WriteLine("0 - Exit");
    Console.WriteLine();
}

static async Task<bool> ShowLiveTransactionSummaryAsync(
    EpiasClient client,
    TransactionSummaryService summaryService,
    ConsoleTablePrinter printer)
{
    Console.WriteLine();

    var items = await client.GetTransactionHistoryAsync();
    if (items.Count == 0)
    {
        Console.WriteLine("No live transaction data available for the selected date range.");
        return false;
    }

    Console.WriteLine($"Loaded {items.Count} live transaction rows.");

    List<ContractSummary> summaries;
    try
    {
        summaries = summaryService.BuildSummaries(items);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Could not build transaction summaries: {ex.Message}");
        return false;
    }

    Console.WriteLine($"Built {summaries.Count} contract summaries.");
    printer.PrintTransactionSummaries(summaries);
    return true;
}

static void ShowLocalTransactionSummary(
    TransactionSummaryService summaryService,
    ConsoleTablePrinter printer)
{
    Console.WriteLine();
    Console.WriteLine("Reading local EPİAŞ JSON file...");

    if (!TryFindEpiasRawJsonPath(out var jsonPath))
    {
        Console.WriteLine("epias_raw.json was not found.");
        Console.WriteLine("Place epias_raw.json in one of these locations:");
        Console.WriteLine($"  1) {Path.Combine(Directory.GetCurrentDirectory(), "epias_raw.json")}");
        Console.WriteLine($"  2) {Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "SmartPulse-Coding-Assignment-main", "epias_raw.json"))}");
        return;
    }

    Console.WriteLine($"Using file: {jsonPath}");

    string json;
    try
    {
        json = File.ReadAllText(jsonPath);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Could not read epias_raw.json: {ex.Message}");
        return;
    }

    TransactionHistoryJsonRoot? root;
    try
    {
        root = JsonSerializer.Deserialize<TransactionHistoryJsonRoot>(json, CreateJsonOptions());
    }
    catch (JsonException ex)
    {
        Console.WriteLine("JSON could not be parsed.");
        Console.WriteLine(ex.Message);
        return;
    }

    var items = root?.Items;
    if (items == null || items.Count == 0)
    {
        Console.WriteLine("No transaction data found in epias_raw.json.");
        return;
    }

    Console.WriteLine($"Loaded {items.Count} transaction rows.");

    List<ContractSummary> summaries;
    try
    {
        summaries = summaryService.BuildSummaries(items);
    }
    catch (Exception ex)
    {
        Console.WriteLine($"Could not build transaction summaries: {ex.Message}");
        return;
    }

    Console.WriteLine($"Built {summaries.Count} contract summaries.");
    printer.PrintTransactionSummaries(summaries);
}

static async Task TestEpiasTgtLoginAsync(EpiasClient client)
{
    Console.WriteLine();
    Console.WriteLine("Testing EPİAŞ TGT login...");

    var firstTgt = await client.GetTgtAsync();
    if (firstTgt == null)
    {
        Console.WriteLine("TGT test failed.");
        return;
    }

    Console.WriteLine("TGT test succeeded.");

    var secondTgt = await client.GetTgtAsync();
    if (secondTgt == null)
    {
        Console.WriteLine("TGT cache test failed on second call.");
        return;
    }

    Console.WriteLine("Cache test completed.");
}

static bool TryFindEpiasRawJsonPath(out string jsonPath)
{
    var candidates = new[]
    {
        Path.Combine(Directory.GetCurrentDirectory(), "epias_raw.json"),
        Path.GetFullPath(Path.Combine(Directory.GetCurrentDirectory(), "..", "SmartPulse-Coding-Assignment-main", "epias_raw.json"))
    };

    foreach (var candidate in candidates)
    {
        if (File.Exists(candidate))
        {
            jsonPath = candidate;
            return true;
        }
    }

    jsonPath = string.Empty;
    return false;
}

static async Task ShowKgupAsync(EpiasClient client, ConsoleTablePrinter printer)
{
    var response = await client.GetKgupAsync();
    printer.PrintKgup(response.Items);
}

static async Task ShowGipIslemHacmiAsync(EpiasClient client, ConsoleTablePrinter printer)
{
    var response = await client.GetGipIslemHacmiAsync();
    printer.PrintTradeValues(response.Items);
}

static async Task ShowAllAsync(
    EpiasClient client,
    TransactionSummaryService summaryService,
    ConsoleTablePrinter printer)
{
    var liveSucceeded = await ShowLiveTransactionSummaryAsync(client, summaryService, printer);
    if (!liveSucceeded)
    {
        Console.WriteLine("Live transaction summary could not be completed. Continuing with remaining options.");
    }

    await ShowKgupAsync(client, printer);
    await ShowGipIslemHacmiAsync(client, printer);
}

static JsonSerializerOptions CreateJsonOptions() => new()
{
    PropertyNameCaseInsensitive = true
};

static void PrintStartupStatus(EpiasSettings epias, EpiasCredentials? credentials)
{
    Console.WriteLine();
    Console.WriteLine("EPİAŞ settings loaded.");
    Console.WriteLine($"BaseUrl: {epias.BaseUrl}");
    Console.WriteLine($"AuthUrl: {epias.AuthUrl}");
    Console.WriteLine($"Region: {epias.Region}");

    if (credentials != null)
    {
        Console.WriteLine("Credentials: found");
    }
    else
    {
        Console.WriteLine("Credentials: not found. Set EPIAS_USERNAME and EPIAS_PASSWORD for live API access.");
    }

    Console.WriteLine();
}

static EpiasCredentials? LoadEpiasCredentials()
{
    var username = Environment.GetEnvironmentVariable("EPIAS_USERNAME");
    var password = Environment.GetEnvironmentVariable("EPIAS_PASSWORD");

    var hasUsername = !string.IsNullOrWhiteSpace(username);
    var hasPassword = !string.IsNullOrWhiteSpace(password);

    if (hasUsername && hasPassword)
    {
        return new EpiasCredentials
        {
            Username = username!,
            Password = password!
        };
    }

    if (hasUsername || hasPassword)
    {
        Console.WriteLine("Warning: Both EPIAS_USERNAME and EPIAS_PASSWORD are required for live API access.");
        if (hasUsername)
        {
            Console.WriteLine("EPIAS_USERNAME is set, but EPIAS_PASSWORD is missing.");
        }
        else
        {
            Console.WriteLine("EPIAS_PASSWORD is set, but EPIAS_USERNAME is missing.");
        }
    }

    return null;
}

static AppSettings LoadSettings(out string? warning)
{
    warning = null;
    var path = Path.Combine(AppContext.BaseDirectory, "appsettings.json");
    if (!File.Exists(path))
    {
        warning = $"Warning: appsettings.json not found at {path}. Using default EPİAŞ settings.";
        return new AppSettings();
    }

    try
    {
        var json = File.ReadAllText(path);
        var settings = JsonSerializer.Deserialize<AppSettings>(json, CreateJsonOptions());
        if (settings?.Epias == null)
        {
            warning = "Warning: Epias section is missing in appsettings.json. Using default EPİAŞ settings.";
            return new AppSettings();
        }

        return settings;
    }
    catch (JsonException ex)
    {
        warning = $"Warning: appsettings.json could not be parsed ({ex.Message}). Using default EPİAŞ settings.";
        return new AppSettings();
    }
}
