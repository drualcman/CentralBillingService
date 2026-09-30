namespace CentralBillingService.WPF.ViewModels;

/// <summary>Checks the rectification form before it is sent; returns the first problem, or null when valid.</summary>
public static class RectifyInvoiceFormValidator
{
    public static string? Validate(RectifyInvoiceViewModel form)
    {
        string? errorMessage = null;
        IReadOnlyList<InvoiceLineItem> lines = form.LinesEditor.Lines;

        if (string.IsNullOrWhiteSpace(form.RectificativeSerie))
        {
            errorMessage = "La serie rectificativa es obligatoria.";
        }
        else if (IsSerieOfOriginalInvoice(form.RectificativeSerie, form.OriginalInvoice))
        {
            errorMessage = "La rectificativa debe ir en una serie propia, distinta de la de la factura original.";
        }
        else if (string.IsNullOrWhiteSpace(form.Reason) || form.Reason.Trim().Length < 10)
        {
            errorMessage = "El motivo debe tener al menos 10 caracteres.";
        }
        else if (string.IsNullOrWhiteSpace(form.PaymentReference))
        {
            errorMessage = "La referencia de pago es obligatoria.";
        }
        else if (lines.Count == 0)
        {
            errorMessage = "Añade al menos una línea.";
        }
        else if (lines.Any(line => string.IsNullOrWhiteSpace(line.Description)))
        {
            errorMessage = "Todas las líneas deben tener descripción.";
        }
        else if (lines.Any(line => line.Quantity == 0))
        {
            errorMessage = "La cantidad de cada línea no puede ser cero (usa negativa para anular).";
        }
        else if (lines.Any(line => line.TaxRate < 0 || line.TaxRate > 100))
        {
            errorMessage = "El tipo impositivo de cada línea debe estar entre 0 y 100.";
        }

        return errorMessage;
    }

    /// <summary>Invoice numbers read "{Serie}{Year}…", e.g. SUA2026-0048 belongs to serie SUA.</summary>
    private static bool IsSerieOfOriginalInvoice(string rectificativeSerie, InvoiceResult? originalInvoice) =>
        originalInvoice is not null
        && originalInvoice.InvoiceNumber.StartsWith(
            $"{rectificativeSerie.Trim()}{originalInvoice.IssueDate.Year}", StringComparison.OrdinalIgnoreCase);

    public static string GetDeepMessage(Exception exception)
    {
        Exception innermost = exception;
        while (innermost.InnerException is not null)
        {
            innermost = innermost.InnerException;
        }

        return innermost == exception ? exception.Message : $"{exception.Message}\n→ {innermost.Message}";
    }
}
