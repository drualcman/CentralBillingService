# CentralBillingService.Persistence.SqlServer

SQL Server persistence layer built on Entity Framework Core. Implements the storage interfaces from the Infrastructure project using a split read/write context pattern.

**Depends on:** `CentralBillingService.Infrastructure`
**Depended on by:** `CentralBillingService.AzureFunction.API`, `CentralBillingService.WPF`

---

## Contents

- [Database Contexts](#database-contexts)
- [Entity Model](#entity-model)
- [Migrations](#migrations)
- [Configuration](#configuration)
- [DI Registration](#di-registration)
- [Running Migrations](#running-migrations)

---

## Database Contexts

The project uses separate EF Core contexts for reading and writing (CQRS-lite pattern).

| Context | Interface | Purpose |
|---|---|---|
| `SqlInvoiceWriteContext` | `IInvoiceWriteContext` | Creates and updates invoices |
| `SqlInvoiceReadContext` | `IInvoiceReadContext` | Queries invoices (optimized, no change tracking) |
| `Iso9001Context` | `IIso9001Context` | ISO 9001 audit log tables |

Both invoice contexts point to the same database and tables. The read context uses `AsNoTracking()` throughout for performance.

---

## Entity Model

### Tables

| Entity | Table | Description |
|---|---|---|
| `InvoiceEntity` | `Invoices` | All invoices (both regular and rectificative in one unified table) |
| `InvoiceLineEntity` | `InvoiceLines` | Invoice line items |
| `RectificativeInvoiceEntity` | `RectificativeInvoices` | Rectificative-specific metadata |
| `InvoiceSequenceEntity` | `InvoiceSequences` | Per-serie per-year sequential number counters |
| `AuditLogEntity` | `AuditLogs` | ISO 9001 operation log |
| `IncidentReportEntity` | `IncidentReports` | ISO 9001 error/incident log |

### Key design decisions

- `InvoiceEntity` uses a **unified table** (as of migration `MergeInvoicesUnifiedTable`) — both regular invoices and rectificative invoices share columns, with nullable fields for rectificative-specific data.
- `InvoiceSequenceEntity` provides atomic number reservation via `ROWVERSION` / optimistic concurrency.
- All monetary values are stored as `decimal(18,6)` with explicit currency code columns.
- Hashes are stored as `nvarchar(64)` (SHA-256 hex string).

---

## Migrations

Migrations are code-first, managed via EF Core tooling. Current migration history:

| Migration | Description |
|---|---|
| `20260512083649_InitialCreate` | Initial schema: Invoices, InvoiceLines, Sequences, Audit tables |
| `20260513044126_DropExchangeRateSource` | Removed redundant exchange rate source column |
| `20260514045212_AddRectifiedByToRectificativeInvoice` | Added `RectifiedBy` tracking column |
| `20260514215846_MergeInvoicesUnifiedTable` | Merged regular + rectificative into a single table |

---

## Configuration

The connection string is read from `appsettings.json` under `Database:ConnectionString`:

```json
{
  "Database": {
    "ConnectionString": "Server=.;Database=CBS;Trusted_Connection=True;Encrypt=False;"
  }
}
```

The configuration class is `DatabaseOptions` with `SectionKey = "Database"`.

For local development with the Azure Function, use user secrets:

```bash
dotnet user-secrets set "Database:ConnectionString" "Server=.;Database=CBS;Trusted_Connection=True;"
```

---

## DI Registration

```csharp
services.AddSqlServerPersistence(options =>
{
    configuration.GetSection(DatabaseOptions.SectionKey).Bind(options);
});
```

Registers:
- `IInvoiceReadContext` → `SqlInvoiceReadContext`
- `IInvoiceWriteContext` → `SqlInvoiceWriteContext`
- `IIso9001Context` → `Iso9001Context`
- `DatabaseOptions` in the options system

---

## Running Migrations

Apply all pending migrations:

```bash
cd Src/CentralBillingService.Persistence.SqlServer

dotnet ef database update \
  --startup-project ../CentralBillingService.AzureFunction.API
```

Create a new migration after changing the entity model:

```bash
dotnet ef migrations add YourMigrationName \
  --startup-project ../CentralBillingService.AzureFunction.API
```

The startup project is needed because EF needs to resolve the connection string from the app's configuration.
