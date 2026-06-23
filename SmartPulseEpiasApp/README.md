# SmartPulse EPİAŞ App

A .NET 8 console application that fetches data from the [EPİAŞ Transparency Platform](https://seffaflik.epias.com.tr/) and presents three assignment outputs:

- **GİP İşlem Özeti** — intraday market transaction summary by contract hour
- **KGÜP** — finalized daily production plan by fuel type
- **GİP İşlem Hacmi** — hourly total financial volume of matching bids/offers

The app authenticates with EPİAŞ CAS (TGT), calls live REST APIs, and can also run against a local JSON file for validation.

## Requirements

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- An EPİAŞ Transparency Platform account
- Internet connection (for live API options)

## Configuration

Non-secret settings are in `appsettings.json`:

- `BaseUrl` — electricity service API base URL
- `AuthUrl` — CAS TGT login URL
- `Region` — used for KGÜP (default `TR1`)

**Credentials are never stored in `appsettings.json`.** Set them via environment variables before running:

```powershell
$env:EPIAS_USERNAME="your-username"
$env:EPIAS_PASSWORD="your-password"
```

## How to Run (In PowerShell)

cd C:\...\SmartPulseEpiasApp

$env:EPIAS_USERNAME="your-username"
$env:EPIAS_PASSWORD="your-password"

dotnet build
dotnet run

## GİP İşlem Özeti Calculations

Transactions are grouped by `contractName`. For each contract hour:

- **Total Transaction Amount** = Σ `(price × quantity) / 10`
- **Total Transaction Quantity** = Σ `quantity / 10`
- **Weighted Average Price** = Total Transaction Amount ÷ Total Transaction Quantity

### Contract name parsing

Contract names follow **PHYYAAGGSS** (e.g. `PH24112706`):

- `PH` — prefix
- `24` — year (2024)
- `11` — month
- `27` — day
- `06` — hour (06:00)

Result: **27/11/2024 06:00**

## Local JSON Fallback

Option **6** loads `epias_raw.json` from the current directory or the bundled Java reference folder. Use this to validate summary calculations without EPİAŞ credentials or network access.

## Project Structure

```
SmartPulseEpiasApp/
├── Program.cs                 — menu and orchestration
├── EpiasClient.cs             — TGT auth and EPİAŞ API calls
├── TransactionSummaryService.cs — GİP summary calculations
├── ConsoleTablePrinter.cs     — table output
├── Models.cs                  — DTOs and settings
└── appsettings.json           — non-secret EPİAŞ URLs and region
```
