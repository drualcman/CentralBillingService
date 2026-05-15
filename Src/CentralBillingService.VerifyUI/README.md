# CentralBillingService.VerifyUI

Blazor WebAssembly application that serves as the public invoice verification page. When a CBS invoice is created, a QR code is embedded in it containing a verification URL that points to this app. Scanning the QR code opens a page that calls the CBS API to confirm the invoice's authenticity and hash chain integrity.

**Depends on:** `CentralBillingService.AzureFunction.API` (HTTP, at runtime only)
**Framework:** Blazor WebAssembly, .NET 10

---

## Contents

- [Pages and Components](#pages-and-components)
- [Verification Flow](#verification-flow)
- [Configuration](#configuration)
- [Local Development](#local-development)
- [Deployment](#deployment)

---

## Pages and Components

### `Verify.razor` — `/`

The only meaningful page. Reads `invoiceNumber` and `hash` from the query string, calls the CBS API, and renders the result.

**Query parameters:**

| Parameter | Description |
|---|---|
| `invoiceNumber` | Formatted invoice number (e.g. `F2026-0042`) |
| `hash` | SHA-256 hash of the invoice (hex string) |

**Rendered states:**

| State | What is shown |
|---|---|
| Missing parameters | Warning — URL is incomplete |
| Loading | Spinner while the API call is in flight |
| API error | Error message with HTTP status code (or generic network error) |
| Success | Full verification result (see below) |

**Success result shows:**
- Invoice number
- Document hash match — whether the `hash` parameter matches the stored hash (`DocumentHashMatches`)
- Chain integrity — whether the internal SHA-256 hash chain is intact (`IntegrityVerified`)
- Full hash value
- Overall status badge (green checkmark / red X)
- Optional message from the API

### `StatusBadge.razor`

Reusable component. Renders a green **Yes** badge or a red **No** badge from a `bool` parameter.

```razor
<StatusBadge Value="@result.DocumentHashMatches" />
```

### `VerifyInvoiceResponse` (model)

```csharp
public sealed record VerifyInvoiceResponse(
    string InvoiceNumber,
    bool IsValid,
    string Hash,
    bool DocumentHashMatches,
    bool IntegrityVerified,
    string? Message);
```

Deserialised from the CBS API response (`GET /api/invoices/{invoiceNumber}/verify?hash={hash}`).

---

## Verification Flow

```
QR code scan
    │
    ▼
https://<verify-ui>/
    ?invoiceNumber=F2026-0042
    &hash=abc123...
    │
    ▼  (Blazor WASM)
GET {CbsApiBaseUrl}api/invoices/F2026-0042/verify?hash=abc123...
    │
    ▼  (CBS API)
VerifyInvoiceIntegrityUseCase
    │  checks DocumentHashMatches + IntegrityVerified
    ▼
VerifyInvoiceResponse → rendered in browser
```

The app is purely a browser-side client. It makes one HTTP call to the CBS API and renders the result — no backend of its own.

---

## Configuration

The only required configuration is the CBS API base URL, set in `wwwroot/appsettings.json`:

```json
{
  "CbsApiBaseUrl": "https://<your-function-app>.azurewebsites.net/api/"
}
```

For local development this defaults to `https://localhost:44369/api/` (the local Azure Functions port). For production, update to the deployed Azure Function App URL before publishing.

`Program.cs` reads this value at startup and throws if it is missing:

```csharp
var apiBaseUrl = builder.Configuration["CbsApiBaseUrl"]
    ?? throw new InvalidOperationException("CbsApiBaseUrl is not configured.");
builder.Services.AddScoped(sp =>
    new HttpClient { BaseAddress = new Uri(apiBaseUrl) });
```

---

## Local Development

### Prerequisites
- .NET 10 SDK
- CBS API running locally (see [AzureFunction.API README](../CentralBillingService.AzureFunction.API/README.md))

### Run

```bash
cd Src/CentralBillingService.VerifyUI
dotnet run
```

The app will be available at:
- `https://localhost:7183` (HTTPS)
- `http://localhost:5091` (HTTP)

Test the verification page by appending parameters:

```
https://localhost:7183/?invoiceNumber=F2026-0001&hash=<sha256-hex>
```

The `hash` value can be copied from any `InvoiceResult.Hash` returned by the API.

---

## Deployment

This is a static Blazor WebAssembly app — publish and host the output files on any static host (Azure Static Web Apps, Azure Blob Storage with static website enabled, CDN, etc.).

```bash
cd Src/CentralBillingService.VerifyUI
dotnet publish -c Release -o ./publish
```

The publishable files are in `publish/wwwroot`. Before publishing, update `wwwroot/appsettings.json` with the production CBS API URL, or replace it post-publish before upload.

The `VerifyUiBaseUrl` setting in `CbsOptions` (set on the Azure Function App) must match the deployed URL of this app — it is used by `SystemInvoiceVerificationUrlProvider` to build the verification URLs embedded in QR codes.
