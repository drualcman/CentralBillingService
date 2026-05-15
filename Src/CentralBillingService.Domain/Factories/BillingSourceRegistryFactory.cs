//namespace CentralBillingService.Domain.Factories;

///// <summary>
///// Builds a <see cref="BillingSourceRegistry"/> from application configuration.
/////
///// Each billing source is defined in appsettings.json (or Azure Key Vault
///// for sensitive data like the NIF). The factory reads those values,
///// constructs the domain objects, and returns a ready-to-use registry
///// that gets registered as a singleton in the DI container.
/////
///// appsettings.json structure:
///// {
/////   "BillingSources": [
/////     {
/////       "Secret": "1234567890",
/////       "BillingSource": "web-fotos",
/////       "Issuer": {
/////         "LegalName": "Tu Nombre Completo",
/////         "TradeName": "MiWebFotos",
/////         "TaxIdValue": "12345678Z",
/////         "TaxIdCountryCode": "ES",
/////         "Email": "facturacion@miweb.com",
/////         "Phone": "+34 600 000 000",
/////         "Website": "https://fotos.miweb.com",
/////         "AddressLine1": "Calle Mayor 1",
/////         "City": "Madrid",
/////         "PostalCode": "28001",
/////         "AddressCountryCode": "ES"
/////       }
/////     },
/////     {
/////       "Secret": "0987654321",
/////       "BillingSource": "web-cripto",
/////       "Issuer": { ... }
/////     }
/////   ]
///// }
///// </summary>
//public static class BillingSourceRegistryFactory
//{
//    public static BillingSourceRegistry BuildFromConfiguration(IConfiguration configuration)
//    {
//        var sections = configuration
//            .GetSection("BillingSources")
//            .GetChildren()
//            .ToList();

//        if (sections.Count == 0)
//            throw new InvalidOperationException(
//                "No billing sources found in configuration. " +
//                "Check the 'BillingSources' section in appsettings.json.");

//        var configs = sections.Select(BuildConfig).ToList();

//        return new BillingSourceRegistry(configs);
//    }

//    private static BillingSourceConfig BuildConfig(IConfigurationSection section)
//    {
//        var secret = section["Secret"]
//            ?? throw new InvalidOperationException(
//                $"Missing 'Secret' key in configuration section '{section.Path}'.");

//        var billingSource = section["BillingSource"]
//            ?? throw new InvalidOperationException(
//                $"Missing 'BillingSource' key in configuration section '{section.Path}'.");

//        var invoiceSerie = section["InvoiceSerie"]
//            ?? throw new InvalidOperationException(
//                $"Missing 'InvoiceSerie' for billing source '{billingSource}'.");

//        var issuerSection = section.GetSection("Issuer");
//        var issuer = BuildIssuer(issuerSection, billingSource);

//        return new BillingSourceConfig
//        {
//            Secret = secret,
//            BillingSource = billingSource,
//            Issuer = issuer,
//        };
//    }

//    private static BillingParty BuildIssuer(IConfigurationSection section, string billingSource)
//    {
//        string Require(string key) =>
//            section[key] ?? throw new InvalidOperationException(
//                $"Missing 'Issuer.{key}' for billing source '{billingSource}'.");

//        var taxId = TaxId.Create(
//            value: Require("TaxIdValue"),
//            countryCode: Require("TaxIdCountryCode"));

//        var address = PostalAddress.Create(
//            line1: Require("AddressLine1"),
//            city: Require("City"),
//            postalCode: Require("PostalCode"),
//            countryCode: Require("AddressCountryCode"),
//            line2: section["AddressLine2"],
//            province: section["Province"]);

//        return BillingParty.Create(
//            legalName: Require("LegalName"),
//            taxId: taxId,
//            address: address,
//            email: Require("Email"),
//            tradeName: section["TradeName"],
//            phone: section["Phone"],
//            website: section["Website"]);
//    }
//}
