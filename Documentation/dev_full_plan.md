# Central Billing Service — Detailed Development Plan

## Objective

A centralized and immutable billing service, compatible with future VeriFactu integrations, consumed by multiple SaaS platforms.

---

# PHASE 1 — Functional and Fiscal Design

Before writing any code.

---

## Step 1 — Define Exact Scope

### The system WILL

- Create finalized invoices
- Generate legal numbering
- Generate chained hashes
- Store immutable fiscal snapshots
- Generate PDFs asynchronously
- Send emails asynchronously
- Allow invoice queries
- Prepare for VeriFactu

---

### The system WILL NOT

- Manage customers
- Modify invoices
- Use drafts
- Handle checkout
- Process payments
- Manage subscriptions
- Automatically handle refunds
- Contain platform business logic

---

## Step 2 — Define Exact Flow

### Official Flow

```text
Payment Success
    ->
POST /invoices
    ->
Invoice persisted
    ->
Invoice number assigned
    ->
Hash generated
    ->
Snapshot stored
    ->
Queue PDF generation
    ->
Queue email
    ->
Queue VeriFactu
    ->
Return invoice data
```

---

## Step 3 — Define Fiscal Model

### Invoice Series

Example:

```text
SHOTUP-2026-000001
COINDPH-2026-000001
TV-2026-000001
```

### Supported Currencies

```text
EUR
USD
PHP
```

### Tax Rules

Decide:

- VAT included or excluded
- Reverse charge
- Tax exempt
- Future OSS support

---

## Step 4 — Define Official Payload

Freeze API contract.

### CreateInvoiceRequest

```json
{
  "platform": "SHOTUP",
  "externalReference": "PAYMENT-123",
  "currency": "EUR",
  "customer": {
    "name": "",
    "taxId": "",
    "email": "",
    "address": "",
    "countryCode": ""
  },
  "lines": [
    {
      "description": "",
      "quantity": 1,
      "unitPrice": 10.00,
      "taxPercent": 21
    }
  ]
}
```

---

# PHASE 2 — Solution Architecture

## Step 5 — Create Solution

Recommended:

```text
Shotup.Billing.sln
```

---

## Step 6 — Create Projects

### APIs

```text
Shotup.Billing.Api
```

### Domain

```text
Shotup.Billing.Domain
```

### Infrastructure

```text
Shotup.Billing.Infrastructure
```

### Contracts

```text
Shotup.Billing.Contracts
```

### PDF

```text
Shotup.Billing.Pdf
```

### VeriFactu

```text
Shotup.Billing.VeriFactu
```

---

## Step 7 — Decide Technical Stack

### Backend

- ASP.NET Core .NET 9
- Minimal API

### Database

- SQL Server

### Async Queue

Initially:

```text
Azure Queue Storage
```

### PDFs

Recommended:

```text
QuestPDF
```

### Storage

```text
Azure Blob Storage
```

---

# PHASE 3 — Database

## Step 8 — Design Tables

### invoice_headers

```text
InvoiceId
InvoiceNumber
Platform
IssueDateUtc

Currency

Subtotal
TaxTotal
GrandTotal

CustomerName
CustomerTaxId
CustomerEmail
CustomerAddress
CustomerCountryCode

PreviousInvoiceHash
InvoiceHash

SnapshotJson

CreatedUtc
```

---

### invoice_lines

```text
InvoiceLineId
InvoiceId

Description
Quantity
UnitPrice
TaxPercent
TaxAmount
LineTotal
```

---

### invoice_series

```text
SeriesId
Platform
Year
CurrentNumber
```

---

### invoice_events

```text
InvoiceEventId
InvoiceId
EventType
EventDataJson
CreatedUtc
```

---

### invoice_processing

Mutable operational data.

```text
InvoiceId

PdfGeneratedUtc
EmailSentUtc
VeriFactuSentUtc

LastError
```

---

## Step 9 — Indexes

### invoice_headers indexes

```text
InvoiceNumber
Platform
IssueDateUtc
ExternalReference
```

---

## Step 10 — Constraints

### Unique Constraints

```text
InvoiceNumber
InvoiceHash
```

---

# PHASE 4 — Numbering Engine

## Step 11 — Implement Atomic Number Generator

Critical for concurrency safety.

### Recommended SQL

```sql
UPDATE invoice_series
SET CurrentNumber = CurrentNumber + 1
OUTPUT INSERTED.CurrentNumber
```

Inside a transaction.

---

## Step 12 — Invoice Number Formatter

Example:

```text
SHOTUP-2026-000001
```

---

# PHASE 5 — Fiscal Snapshot

## Step 13 — Create Internal Fiscal Model

Do NOT use external DTOs directly.

### InternalFiscalInvoiceSnapshot

Must contain:

- All fiscal data
- Final calculated values
- Immutable state

---

## Step 14 — Serialize Snapshot

Store full JSON.

Example:

```json
{
  "invoiceNumber": "",
  "customer": {},
  "lines": [],
  "totals": {}
}
```

---

## Step 15 — Generate Hash

### Recommended Algorithm

```text
SHA256
```

### Hash Input

```text
snapshot_json + previous_invoice_hash
```

---

## Step 16 — Chain Invoices

Next invoice uses:

```text
previous = last invoice hash
```

---

# PHASE 6 — Main API

## Step 17 — POST /invoices

### Validate

- Lines
- Taxes
- Currency
- Email
- Totals

### Generate

- Number
- Snapshot
- Hash

### Persist

Everything inside ONE transaction.

### Queue

```text
GeneratePdf
SendEmail
SendVeriFactu
```

### Immediate Response

```json
{
  "invoiceId": "",
  "invoiceNumber": "",
  "pdfUrl": ""
}
```

---

## Step 18 — GET /invoices

Filtered by platform.

---

## Step 19 — GET /invoices/{id}

Complete detail.

---

# PHASE 7 — Security

## Step 20 — API Keys

Each platform will have:

```text
PlatformId
ApiKey
IsActive
```

---

## Step 21 — Authentication Middleware

Extract:

```http
X-Api-Key
```

---

## Step 22 — Multi-Tenant Isolation

Every query:

```sql
WHERE Platform = @Platform
```

---

# PHASE 8 — PDF Engine

## Step 23 — GeneratePdf Worker

Reads queue:

```text
GeneratePdf
```

---

## Step 24 — Generate PDF

Using:

```text
QuestPDF
```

---

## Step 25 — Upload to Blob Storage

Path:

```text
/invoices/2026/SHOTUP-2026-000001.pdf
```

---

## Step 26 — Register Event

```text
PdfGenerated
```

---

# PHASE 9 — Email

## Step 27 — SendEmail Worker

---

## Step 28 — Email Template

Keep it simple.

---

## Step 29 — Attach PDF

Or signed URL.

---

## Step 30 — Register Event

```text
EmailSent
```

---

# PHASE 10 — VeriFactu Preparation

## Step 31 — Design Internal VeriFactu Model

Even before official integration.

---

## Step 32 — Create XML Mapper

Internal only.

---

## Step 33 — Create Fiscal Signature

Separate from internal hash.

---

## Step 34 — Create VeriFactu Queue

```text
SendVeriFactu
```

---

## Step 35 — Register Traceability Events

```text
VeriFactuQueued
```

---

# PHASE 11 — Auditing

## Step 36 — Integrity Check Endpoint

```http
POST /internal/integrity-check
```

---

## Step 37 — Recalculate Hashes

Compare:

```text
stored vs recalculated
```

---

## Step 38 — Store Results

---

# PHASE 12 — Observability

## Step 39 — Structured Logging

Use:

```text
ILogger
```

With scopes.

---

## Step 40 — Correlation IDs

Very important.

---

## Step 41 — Application Insights

---

# PHASE 13 — Deployment

## Step 42 — Azure Resources

### App Service or Functions

### SQL Server

### Blob Storage

### Queue Storage

### Key Vault

---

## Step 43 — Secrets Management

Move:

- Connection strings
- API keys
- SMTP credentials
- Future AEAT certificates

To:

```text
Azure Key Vault
```

---

# PHASE 14 — Platform Integration

## Step 44 — Create Internal Billing SDK

Recommended.

### Shotup.Billing.Client

Methods:

```text
CreateInvoiceAsync()
GetInvoicesAsync()
GetInvoiceAsync()
```

---

## Step 45 — Integrate Payment Webhooks

Each platform:

```text
Payment success
    ->
Billing Client
```

---

# PHASE 15 — Legal Hardening

## Step 46 — Block UPDATE

Recommended.

### Strategy

DENY UPDATE on:

```sql
invoice_headers
invoice_lines
```

---

## Step 47 — INSERT Only

Append-only model.

---

## Step 48 — Automatic Backups

Critical.

---

## Step 49 — Nightly Integrity Job

Recalculate hashes.

---

## Step 50 — Documentation

Very important.

### OpenAPI

### Legal Flow

### Rectification Policy

### Immutability Policy