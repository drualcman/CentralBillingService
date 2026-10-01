# CLAUDE.md — CentralBillingService (CBS)

Read this file before touching anything in this repository. It applies to every developer and every AI assistant.

---

## ⛔ VERIFACTU FREEZE: FROM 1 JANUARY 2027 THE CODE AND THE DATABASE ARE FROZEN

From **1 January 2027** (the moment real invoices start to be registered with the AEAT), everything
that takes part in VeriFactu is **FROZEN AND IMMUTABLE**: the code AND the database.

If a billing source is switched to VeriFactu earlier (voluntary submission), the freeze applies to it
**from its first accepted submission**, not from 2027.

### Why

Every fiscal record is signed and chained: each record carries the *huella* (SHA-256) of the previous
one, and every invoice carries its own integrity hash. Once records are sent to the AEAT:

- Changing how a hash or *huella* is computed breaks the chain. Records already sent can no longer be
  verified, and the software stops complying (RD 1007/2023, Orden HAC/1177/2024).
- An accepted record is final. It cannot be edited or deleted. It can only be corrected with a
  rectificative invoice, a cancellation record (*anulación*) or a correction record (*subsanación*).
- A broken chain cannot be repaired afterwards and cannot be justified to the tax authority.

### The only exception

A **change in the regulations** (a new Royal Decree, Ministerial Order or AEAT technical specification)
that **requires or explicitly allows** the change. Even then:

1. The project owner must approve it explicitly, citing the regulation.
2. The change is **additive and versioned**: new behaviour applies only to new records. Records already
   issued keep being verified with the algorithm they were created with. History is never rewritten.
3. The frozen tests below must keep passing unchanged for the old records.

"It would be cleaner", "it's a small refactor", "it's a bug fix", "the code style rules say so" or
"just renaming" are **NOT** valid reasons. That includes the C# style rules: frozen code is not
refactored to match them.

### What is frozen (code)

| Area | Files |
|---|---|
| Invoice integrity hash (canonical string, field order, separator, formats) | `Src/CentralBillingService.Infrastructure/Hashing/Sha256InvoiceHasher.cs`, `Src/CentralBillingService.Domain/Interfaces/IInvoiceHasher.cs`, `Src/CentralBillingService.Domain/Models/InvoiceHashContent.cs`, `Src/CentralBillingService.Domain/Models/InvoiceLineHashContent.cs` |
| Hash inputs held by the entities (what feeds the hash, `PreviousHash` chaining, `VerifyIntegrity`) | `Src/CentralBillingService.Domain/Entities/Invoice.cs`, `RectificativeInvoice.cs`, `InvoiceLine.cs`, and the value objects they hash (`InvoiceNumber`, `Money`, `TaxRate`, `TaxId`, `PostalAddress`, `ExchangeRate`, `Currency`) |
| Invoice numbering (format, series, sequences) | `Src/CentralBillingService.Domain/ValueObjects/InvoiceNumber.cs`, `Src/CentralBillingService.Infrastructure/NumberProviders/*`, `FiscalSerieGuard.cs`, `RectificativeSerieGuard.cs` |
| VeriFactu adapter (record mapping, *huella*, chain, signature, transport, response handling) | everything under `Src/CentralBillingService.VeriFactu/` |
| Fiscal registration flow | `IFiscalRegistrar*`, `FiscalRegistrarFactory`, `NoneFiscalRegistrar*`, `SubmitFiscalRecordUseCase`, `RetryStalledFiscalSubmissionsUseCase`, `ProcessFiscalSubmissionFunction`, `RetryStalledFiscalSubmissionsFunction`, `FiscalStampResult`, `FiscalSubmissionOutcome`, `FiscalSubmissionState`, `RectifiedInvoiceAmounts` |
| Fiscal QR (content, AEAT URL, size, position, "VERI*FACTU" legend) | `GenerateInvoiceQrUseCase`, `GenerateInvoiceQrHandler`, `QrCodeGenerator`, `Src/CentralBillingService.Reports/Builders/FiscalQr.cs` and the fiscal-QR cells in the report headers |
| `VeriFactu` NuGet package (mdiago) | pinned at **1.0.68**. It builds the XML, the *huella* and the signature. Update it only for a regulatory change, and only after proving that the same input produces the same *huella* |

### What is frozen (database)

Tables: `Invoices`, `InvoiceLines`, `InvoiceSequences`, `VeriFactuChain`, `VeriFactuSubmission`.

- **No** `UPDATE` or `DELETE` on issued invoices, lines, chain links or submissions. No manual scripts, no
  "data fixes", no backfills.
- **No** migrations that drop, rename, retype, shorten or recompute a column that is hashed, chained or
  sent to the AEAT. That includes migrations removed or rewritten after they were applied.
- **No** resetting or reseeding sequences or the chain.
- A new nullable column that is outside the hash, the chain and the AEAT record (as `Layout` is) is
  allowed only with the owner's explicit approval.
- Never run migrations against production from a development machine.

### Frozen tests

`Tests/CentralBillingService.Tests/Unit/VeriFactu/*` and the hash, integrity and numbering tests under
`Tests/CentralBillingService.Tests/Unit/Domain` and `Unit/Infrastructure` are the contract. They are
**never** edited to make a change pass. If one fails, the change is wrong.

### Still allowed after the freeze

Anything outside the fiscal content: report presentation (layouts, fonts, colours, positions, except for
the fiscal QR and its legend), the WPF and VerifyUI screens, e-mail, new non-fiscal features, logging,
and configuration (certificates, endpoints, billing sources). New fields must **never** enter the hash.
Before doing any of these, confirm that the change does not reach any file or table listed above.

### Rules for AI assistants

- Before editing, check whether the file, table or migration is in the lists above. If it is, and the
  date is 1 January 2027 or later (or the billing source is already live on VeriFactu): **STOP**. Do
  not edit. Explain the freeze to the user and ask for the regulation that allows the change.
- Never propose to "fix" a historical invoice or fiscal record in place. The correct paths are a
  rectificative invoice, a cancellation record or a correction record.
- If you are unsure whether something is covered: treat it as frozen.
