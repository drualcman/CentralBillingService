# CentralBillingService.WPF

The Central Control desktop application. A Windows WPF app built with Material Design that gives billing administrators a full UI for managing invoices, billing sources, clients, products, and series.

**Platform:** Windows only (.NET 10 Windows)
**Depends on:** `CentralBillingService.Application`, `CentralBillingService.Infrastructure`, `CentralBillingService.Persistence.SqlServer`

> This application connects directly to the same backend layers as the Azure Function API. It does **not** go through the HTTP API — it uses the use cases and repositories directly via DI.

---

## Contents

- [Features](#features)
- [Views and ViewModels](#views-and-viewmodels)
- [Master Data](#master-data)
- [Architecture](#architecture)
- [Configuration](#configuration)
- [Running the App](#running-the-app)

---

## Features

- Create invoices with real-time total calculation
- View invoice details including hash, integrity status, and exchange rate
- List and filter invoices
- Issue rectificative invoices (credit notes / corrections)
- Verify invoice integrity (SHA-256 chain check)
- Manage master data: clients, invoice series, products
- Material Design 3 UI with status-aware color coding

---

## Views and ViewModels

The app follows MVVM using `CommunityToolkit.Mvvm`. Each view has a corresponding ViewModel that holds all state and commands.

### Invoice Creation — `CreateInvoiceViewModel`

Manages the invoice creation form.

**State properties:**
- `Serie` — selected serie (from master data)
- `OriginCurrencyCode` — origin currency (blank = EUR)
- `IssueDate`, `ValueDate` — date pickers
- `Notes`, `PaymentMethod`, `PaymentReference`, `TransactionData`
- Recipient fields: `LegalName`, `TradeName`, `TaxId`, `TaxIdCountry`, `Email`, `Phone`, address fields
- `Lines` — `ObservableCollection<InvoiceLineItem>` (dynamically add/remove lines)
- `TotalsSubtotal`, `TotalsTax`, `TotalsTotal` — live-calculated totals
- `IsSaving`, `ErrorMessage`, `Success`

**Commands:**
- `SaveCommand` — validates and submits the invoice
- `AddLineCommand` / `RemoveLineCommand` — manage line items
- `SaveClientToMasterCommand` — saves the current recipient to local client master data
- `SaveProductToMasterCommand` — saves a product/service description to master data
- `CancelCommand` — navigates back

---

### Invoice Detail — `InvoiceDetailViewModel`

Displays all fields of a single invoice including hash, integrity status, and applied exchange rate.

**Highlights:**
- `HasTamper` is surfaced with a prominent warning color (via `StatusToBrushConverter`)
- Exchange rate details shown when origin currency ≠ EUR
- Navigation back to list via `GoBackCommand`

---

### Invoice List — `InvoicesViewModel` (backing `InvoicesView`)

Paginated list of invoices with filter controls. Selecting an invoice navigates to the detail view.

---

### Rectify Invoice — `RectifyInvoiceViewModel`

Form for issuing a rectificative invoice against an existing one.

**Inputs:**
- Rectificative serie, rectification type (`Substitution` / `Difference`), reason
- New lines (same line editor as creation)
- Payment info

---

### Verify Invoice — `VerifyInvoiceViewModel`

Checks invoice integrity without a provided hash (administrator flow).

**Outputs:**
- `IntegrityVerified` — whether the SHA-256 chain is intact
- Error details if tampering is detected

---

### Master Data — `MasterDataViewModel`

Manages the local master data store for quick selection during invoice creation.

| Entity | Fields |
|---|---|
| `ClientRecord` | Legal name, trade name, tax ID, email, phone, address |
| `SeriesRecord` | Serie code, description |
| `ProductRecord` | Description, default unit price, default tax rate |

---

## Master Data

Master data is stored locally (not in the billing database) to speed up invoice entry. It is maintained per installation.

| Store class | Responsibility |
|---|---|
| `LocalMasterDataStore` | Load and save clients, series, and products to local storage |

Master data is loaded at startup and available as observable collections in `CreateInvoiceViewModel`.

---

## Architecture

The WPF app uses `Microsoft.Extensions.Hosting` for DI, logging, and configuration — the same infrastructure pattern as the Azure Function.

```csharp
// Startup (App.xaml.cs)
Host.CreateDefaultBuilder()
    .ConfigureServices((ctx, services) =>
    {
        services
            .AddBillingDomain(ctx.Configuration.GetSection(...).Bind)
            .AddBillingApplication()
            .AddBillingInfrastructure()
            .AddSqlServerPersistence(ctx.Configuration.GetSection(...).Bind)
            .AddWpfViewModels();
    });
```

ViewModels receive `IServiceScopeFactory` and create a new DI scope per operation to avoid holding long-lived DbContext instances.

### Value Converters

| Converter | Purpose |
|---|---|
| `StatusToBrushConverter` | Maps `InvoiceStatus` to a `Brush` for color coding |
| `StatusToLabelConverter` | Maps `InvoiceStatus` to a display string |
| `BoolToVisibilityConverter` | Maps `bool` to `Visibility` |
| `NullToVisibilityConverter` | Hides elements when a binding is null |

---

## Configuration

`appsettings.json` (copied to output directory):

```json
{
  "Database": {
    "ConnectionString": "Server=.;Database=CBS;Trusted_Connection=True;Encrypt=False;"
  },
  "Cbs": {
    "BillingSources": [
      {
        "Name": "default",
        "Secret": "your-secret"
      }
    ]
  }
}
```

The app reads from `appsettings.json` in its output directory. For development, place a `appsettings.Development.json` alongside it.

---

## Running the App

```bash
cd Src/CentralBillingService.WPF
dotnet run
```

Or build and run the executable from `bin/Release/net10.0-windows/`.

### Prerequisites
- Windows (required for WPF)
- .NET 10 Desktop Runtime (or SDK for development)
- SQL Server reachable at the configured connection string
- Database must be up to date with all migrations (run from the Persistence project or the Function API project)
