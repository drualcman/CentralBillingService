namespace CentralBillingService.Domain.ValueObjects;

/// <summary>
/// Represents a currency according to the ISO 4217 standard.
/// Lightweight immutable value object — equality is based solely on the currency code.
/// Contains all 154 active ISO 4217 currencies as of 2025.
/// </summary>
public sealed class Currency
{
    public string Code { get; }   // EUR, USD, PHP, JPY...
    public string Name { get; }   // Euro, US Dollar, Philippine Peso...
    public string Symbol { get; }   // €, $, ₱, ¥...
    public int DecimalPlaces { get; }  // 2 for most, 0 for JPY/KRW, 3 for KWD/BHD

    private Currency(string code, string name, string symbol, int decimalPlaces)
    {
        Code = code;
        Name = name;
        Symbol = symbol;
        DecimalPlaces = decimalPlaces;
    }

    // -------------------------------------------------------------------------
    // A – Active ISO 4217 currencies (154 codes, ordered alphabetically by code)
    // -------------------------------------------------------------------------

    // --- A ---
    public static readonly Currency AED = new("AED", "UAE Dirham", "د.إ", 2);
    public static readonly Currency AFN = new("AFN", "Afghan Afghani", "؋", 2);
    public static readonly Currency ALL = new("ALL", "Albanian Lek", "L", 2);
    public static readonly Currency AMD = new("AMD", "Armenian Dram", "֏", 2);
    public static readonly Currency ANG = new("ANG", "Netherlands Antillean Guilder", "ƒ", 2);
    public static readonly Currency AOA = new("AOA", "Angolan Kwanza", "Kz", 2);
    public static readonly Currency ARS = new("ARS", "Argentine Peso", "$", 2);
    public static readonly Currency AUD = new("AUD", "Australian Dollar", "A$", 2);
    public static readonly Currency AWG = new("AWG", "Aruban Florin", "ƒ", 2);
    public static readonly Currency AZN = new("AZN", "Azerbaijani Manat", "₼", 2);

    // --- B ---
    public static readonly Currency BAM = new("BAM", "Bosnia-Herzegovina Convertible Mark", "KM", 2);
    public static readonly Currency BBD = new("BBD", "Barbadian Dollar", "Bds$", 2);
    public static readonly Currency BDT = new("BDT", "Bangladeshi Taka", "৳", 2);
    public static readonly Currency BGN = new("BGN", "Bulgarian Lev", "лв", 2);
    public static readonly Currency BHD = new("BHD", "Bahraini Dinar", "BD", 3);
    public static readonly Currency BIF = new("BIF", "Burundian Franc", "Fr", 0);
    public static readonly Currency BMD = new("BMD", "Bermudan Dollar", "BD$", 2);
    public static readonly Currency BND = new("BND", "Brunei Dollar", "B$", 2);
    public static readonly Currency BOB = new("BOB", "Bolivian Boliviano", "Bs.", 2);
    public static readonly Currency BRL = new("BRL", "Brazilian Real", "R$", 2);
    public static readonly Currency BSD = new("BSD", "Bahamian Dollar", "B$", 2);
    public static readonly Currency BTN = new("BTN", "Bhutanese Ngultrum", "Nu", 2);
    public static readonly Currency BWP = new("BWP", "Botswanan Pula", "P", 2);
    public static readonly Currency BYN = new("BYN", "Belarusian Ruble", "Br", 2);
    public static readonly Currency BZD = new("BZD", "Belize Dollar", "BZ$", 2);

    // --- C ---
    public static readonly Currency CAD = new("CAD", "Canadian Dollar", "CA$", 2);
    public static readonly Currency CDF = new("CDF", "Congolese Franc", "Fr", 2);
    public static readonly Currency CHF = new("CHF", "Swiss Franc", "Fr", 2);
    public static readonly Currency CLP = new("CLP", "Chilean Peso", "$", 0);
    public static readonly Currency CNY = new("CNY", "Chinese Yuan Renminbi", "¥", 2);
    public static readonly Currency COP = new("COP", "Colombian Peso", "$", 2);
    public static readonly Currency CRC = new("CRC", "Costa Rican Colón", "₡", 2);
    public static readonly Currency CUP = new("CUP", "Cuban Peso", "$MN", 2);
    public static readonly Currency CVE = new("CVE", "Cape Verdean Escudo", "$", 2);
    public static readonly Currency CZK = new("CZK", "Czech Koruna", "Kč", 2);

    // --- D ---
    public static readonly Currency DJF = new("DJF", "Djiboutian Franc", "Fdj", 0);
    public static readonly Currency DKK = new("DKK", "Danish Krone", "kr", 2);
    public static readonly Currency DOP = new("DOP", "Dominican Peso", "RD$", 2);
    public static readonly Currency DZD = new("DZD", "Algerian Dinar", "دج", 2);

    // --- E ---
    public static readonly Currency EGP = new("EGP", "Egyptian Pound", "E£", 2);
    public static readonly Currency ERN = new("ERN", "Eritrean Nakfa", "Nfk", 2);
    public static readonly Currency ETB = new("ETB", "Ethiopian Birr", "Br", 2);
    public static readonly Currency EUR = new("EUR", "Euro", "€", 2);

    // --- F ---
    public static readonly Currency FJD = new("FJD", "Fijian Dollar", "FJ$", 2);
    public static readonly Currency FKP = new("FKP", "Falkland Islands Pound", "FK£", 2);

    // --- G ---
    public static readonly Currency GBP = new("GBP", "British Pound Sterling", "£", 2);
    public static readonly Currency GEL = new("GEL", "Georgian Lari", "₾", 2);
    public static readonly Currency GHS = new("GHS", "Ghanaian Cedi", "GH₵", 2);
    public static readonly Currency GIP = new("GIP", "Gibraltar Pound", "£", 2);
    public static readonly Currency GMD = new("GMD", "Gambian Dalasi", "D", 2);
    public static readonly Currency GNF = new("GNF", "Guinean Franc", "Fr", 0);
    public static readonly Currency GTQ = new("GTQ", "Guatemalan Quetzal", "Q", 2);
    public static readonly Currency GYD = new("GYD", "Guyanese Dollar", "G$", 2);

    // --- H ---
    public static readonly Currency HKD = new("HKD", "Hong Kong Dollar", "HK$", 2);
    public static readonly Currency HNL = new("HNL", "Honduran Lempira", "L", 2);
    public static readonly Currency HTG = new("HTG", "Haitian Gourde", "G", 2);
    public static readonly Currency HUF = new("HUF", "Hungarian Forint", "Ft", 2);

    // --- I ---
    public static readonly Currency IDR = new("IDR", "Indonesian Rupiah", "Rp", 2);
    public static readonly Currency ILS = new("ILS", "Israeli New Shekel", "₪", 2);
    public static readonly Currency INR = new("INR", "Indian Rupee", "₹", 2);
    public static readonly Currency IQD = new("IQD", "Iraqi Dinar", "ع.د", 3);
    public static readonly Currency IRR = new("IRR", "Iranian Rial", "﷼", 2);
    public static readonly Currency ISK = new("ISK", "Icelandic Króna", "kr", 0);

    // --- J ---
    public static readonly Currency JMD = new("JMD", "Jamaican Dollar", "J$", 2);
    public static readonly Currency JOD = new("JOD", "Jordanian Dinar", "JD", 3);
    public static readonly Currency JPY = new("JPY", "Japanese Yen", "¥", 0);

    // --- K ---
    public static readonly Currency KES = new("KES", "Kenyan Shilling", "KSh", 2);
    public static readonly Currency KGS = new("KGS", "Kyrgyzstani Som", "с", 2);
    public static readonly Currency KHR = new("KHR", "Cambodian Riel", "៛", 2);
    public static readonly Currency KMF = new("KMF", "Comorian Franc", "CF", 0);
    public static readonly Currency KPW = new("KPW", "North Korean Won", "₩", 2);
    public static readonly Currency KRW = new("KRW", "South Korean Won", "₩", 0);
    public static readonly Currency KWD = new("KWD", "Kuwaiti Dinar", "KD", 3);
    public static readonly Currency KYD = new("KYD", "Cayman Islands Dollar", "CI$", 2);
    public static readonly Currency KZT = new("KZT", "Kazakhstani Tenge", "₸", 2);

    // --- L ---
    public static readonly Currency LAK = new("LAK", "Laotian Kip", "₭", 2);
    public static readonly Currency LBP = new("LBP", "Lebanese Pound", "ل.ل", 2);
    public static readonly Currency LKR = new("LKR", "Sri Lankan Rupee", "Rs", 2);
    public static readonly Currency LRD = new("LRD", "Liberian Dollar", "L$", 2);
    public static readonly Currency LSL = new("LSL", "Lesotho Loti", "L", 2);
    public static readonly Currency LYD = new("LYD", "Libyan Dinar", "LD", 3);

    // --- M ---
    public static readonly Currency MAD = new("MAD", "Moroccan Dirham", "MAD", 2);
    public static readonly Currency MDL = new("MDL", "Moldovan Leu", "L", 2);
    public static readonly Currency MGA = new("MGA", "Malagasy Ariary", "Ar", 2);
    public static readonly Currency MKD = new("MKD", "Macedonian Denar", "ден", 2);
    public static readonly Currency MMK = new("MMK", "Myanmar Kyat", "K", 2);
    public static readonly Currency MNT = new("MNT", "Mongolian Tögrög", "₮", 2);
    public static readonly Currency MOP = new("MOP", "Macanese Pataca", "P", 2);
    public static readonly Currency MRU = new("MRU", "Mauritanian Ouguiya", "UM", 2);
    public static readonly Currency MUR = new("MUR", "Mauritian Rupee", "Rs", 2);
    public static readonly Currency MVR = new("MVR", "Maldivian Rufiyaa", "Rf", 2);
    public static readonly Currency MWK = new("MWK", "Malawian Kwacha", "MK", 2);
    public static readonly Currency MXN = new("MXN", "Mexican Peso", "$", 2);
    public static readonly Currency MYR = new("MYR", "Malaysian Ringgit", "RM", 2);
    public static readonly Currency MZN = new("MZN", "Mozambican Metical", "MT", 2);

    // --- N ---
    public static readonly Currency NAD = new("NAD", "Namibian Dollar", "N$", 2);
    public static readonly Currency NGN = new("NGN", "Nigerian Naira", "₦", 2);
    public static readonly Currency NIO = new("NIO", "Nicaraguan Córdoba", "C$", 2);
    public static readonly Currency NOK = new("NOK", "Norwegian Krone", "kr", 2);
    public static readonly Currency NPR = new("NPR", "Nepalese Rupee", "Rs", 2);
    public static readonly Currency NZD = new("NZD", "New Zealand Dollar", "NZ$", 2);

    // --- O ---
    public static readonly Currency OMR = new("OMR", "Omani Rial", "ر.ع.", 3);

    // --- P ---
    public static readonly Currency PAB = new("PAB", "Panamanian Balboa", "B/.", 2);
    public static readonly Currency PEN = new("PEN", "Peruvian Sol", "S/", 2);
    public static readonly Currency PGK = new("PGK", "Papua New Guinean Kina", "K", 2);
    public static readonly Currency PHP = new("PHP", "Philippine Peso", "₱", 2);
    public static readonly Currency PKR = new("PKR", "Pakistani Rupee", "Rs", 2);
    public static readonly Currency PLN = new("PLN", "Polish Złoty", "zł", 2);
    public static readonly Currency PYG = new("PYG", "Paraguayan Guaraní", "₲", 0);

    // --- Q ---
    public static readonly Currency QAR = new("QAR", "Qatari Riyal", "QR", 2);

    // --- R ---
    public static readonly Currency RON = new("RON", "Romanian Leu", "lei", 2);
    public static readonly Currency RSD = new("RSD", "Serbian Dinar", "din", 2);
    public static readonly Currency RUB = new("RUB", "Russian Ruble", "₽", 2);
    public static readonly Currency RWF = new("RWF", "Rwandan Franc", "Fr", 0);

    // --- S ---
    public static readonly Currency SAR = new("SAR", "Saudi Riyal", "SR", 2);
    public static readonly Currency SBD = new("SBD", "Solomon Islands Dollar", "SI$", 2);
    public static readonly Currency SCR = new("SCR", "Seychellois Rupee", "Rs", 2);
    public static readonly Currency SDG = new("SDG", "Sudanese Pound", "ج.س.", 2);
    public static readonly Currency SEK = new("SEK", "Swedish Krona", "kr", 2);
    public static readonly Currency SGD = new("SGD", "Singapore Dollar", "S$", 2);
    public static readonly Currency SHP = new("SHP", "Saint Helena Pound", "£", 2);
    public static readonly Currency SLE = new("SLE", "Sierra Leonean Leone", "Le", 2);
    public static readonly Currency SOS = new("SOS", "Somali Shilling", "Sh", 2);
    public static readonly Currency SRD = new("SRD", "Surinamese Dollar", "SR$", 2);
    public static readonly Currency SSP = new("SSP", "South Sudanese Pound", "SSP", 2);
    public static readonly Currency STN = new("STN", "São Tomé and Príncipe Dobra", "Db", 2);
    public static readonly Currency SYP = new("SYP", "Syrian Pound", "£S", 2);
    public static readonly Currency SZL = new("SZL", "Swazi Lilangeni", "L", 2);

    // --- T ---
    public static readonly Currency THB = new("THB", "Thai Baht", "฿", 2);
    public static readonly Currency TJS = new("TJS", "Tajikistani Somoni", "SM", 2);
    public static readonly Currency TMT = new("TMT", "Turkmenistani Manat", "T", 2);
    public static readonly Currency TND = new("TND", "Tunisian Dinar", "DT", 3);
    public static readonly Currency TOP = new("TOP", "Tongan Paʻanga", "T$", 2);
    public static readonly Currency TRY = new("TRY", "Turkish Lira", "₺", 2);
    public static readonly Currency TTD = new("TTD", "Trinidad and Tobago Dollar", "TT$", 2);
    public static readonly Currency TWD = new("TWD", "New Taiwan Dollar", "NT$", 2);
    public static readonly Currency TZS = new("TZS", "Tanzanian Shilling", "Sh", 2);

    // --- U ---
    public static readonly Currency UAH = new("UAH", "Ukrainian Hryvnia", "₴", 2);
    public static readonly Currency UGX = new("UGX", "Ugandan Shilling", "Sh", 0);
    public static readonly Currency USD = new("USD", "US Dollar", "$", 2);
    public static readonly Currency UYU = new("UYU", "Uruguayan Peso", "$U", 2);
    public static readonly Currency UZS = new("UZS", "Uzbekistani Sum", "so'm", 2);

    // --- V ---
    public static readonly Currency VES = new("VES", "Venezuelan Bolívar Soberano", "Bs.S", 2);
    public static readonly Currency VND = new("VND", "Vietnamese Đồng", "₫", 0);
    public static readonly Currency VUV = new("VUV", "Vanuatu Vatu", "VT", 0);

    // --- W ---
    public static readonly Currency WST = new("WST", "Samoan Tala", "WS$", 2);

    // --- X (supranational / regional) ---
    public static readonly Currency XAF = new("XAF", "Central African CFA Franc", "Fr", 0);
    public static readonly Currency XCD = new("XCD", "East Caribbean Dollar", "EC$", 2);
    public static readonly Currency XOF = new("XOF", "West African CFA Franc", "Fr", 0);
    public static readonly Currency XPF = new("XPF", "CFP Franc", "Fr", 0);

    // --- Y ---
    public static readonly Currency YER = new("YER", "Yemeni Rial", "﷼", 2);

    // --- Z ---
    public static readonly Currency ZAR = new("ZAR", "South African Rand", "R", 2);
    public static readonly Currency ZMW = new("ZMW", "Zambian Kwacha", "ZK", 2);
    public static readonly Currency ZWL = new("ZWL", "Zimbabwean Dollar", "Z$", 2);

    // -------------------------------------------------------------------------
    // Lookup dictionary — built once at class initialization
    // -------------------------------------------------------------------------
    private static readonly Dictionary<string, Currency> _all = new()
    {
        [AED.Code] = AED,
        [AFN.Code] = AFN,
        [ALL.Code] = ALL,
        [AMD.Code] = AMD,
        [ANG.Code] = ANG,
        [AOA.Code] = AOA,
        [ARS.Code] = ARS,
        [AUD.Code] = AUD,
        [AWG.Code] = AWG,
        [AZN.Code] = AZN,

        [BAM.Code] = BAM,
        [BBD.Code] = BBD,
        [BDT.Code] = BDT,
        [BGN.Code] = BGN,
        [BHD.Code] = BHD,
        [BIF.Code] = BIF,
        [BMD.Code] = BMD,
        [BND.Code] = BND,
        [BOB.Code] = BOB,
        [BRL.Code] = BRL,
        [BSD.Code] = BSD,
        [BTN.Code] = BTN,
        [BWP.Code] = BWP,
        [BYN.Code] = BYN,
        [BZD.Code] = BZD,

        [CAD.Code] = CAD,
        [CDF.Code] = CDF,
        [CHF.Code] = CHF,
        [CLP.Code] = CLP,
        [CNY.Code] = CNY,
        [COP.Code] = COP,
        [CRC.Code] = CRC,
        [CUP.Code] = CUP,
        [CVE.Code] = CVE,
        [CZK.Code] = CZK,

        [DJF.Code] = DJF,
        [DKK.Code] = DKK,
        [DOP.Code] = DOP,
        [DZD.Code] = DZD,

        [EGP.Code] = EGP,
        [ERN.Code] = ERN,
        [ETB.Code] = ETB,
        [EUR.Code] = EUR,

        [FJD.Code] = FJD,
        [FKP.Code] = FKP,

        [GBP.Code] = GBP,
        [GEL.Code] = GEL,
        [GHS.Code] = GHS,
        [GIP.Code] = GIP,
        [GMD.Code] = GMD,
        [GNF.Code] = GNF,
        [GTQ.Code] = GTQ,
        [GYD.Code] = GYD,

        [HKD.Code] = HKD,
        [HNL.Code] = HNL,
        [HTG.Code] = HTG,
        [HUF.Code] = HUF,

        [IDR.Code] = IDR,
        [ILS.Code] = ILS,
        [INR.Code] = INR,
        [IQD.Code] = IQD,
        [IRR.Code] = IRR,
        [ISK.Code] = ISK,

        [JMD.Code] = JMD,
        [JOD.Code] = JOD,
        [JPY.Code] = JPY,

        [KES.Code] = KES,
        [KGS.Code] = KGS,
        [KHR.Code] = KHR,
        [KMF.Code] = KMF,
        [KPW.Code] = KPW,
        [KRW.Code] = KRW,
        [KWD.Code] = KWD,
        [KYD.Code] = KYD,
        [KZT.Code] = KZT,

        [LAK.Code] = LAK,
        [LBP.Code] = LBP,
        [LKR.Code] = LKR,
        [LRD.Code] = LRD,
        [LSL.Code] = LSL,
        [LYD.Code] = LYD,

        [MAD.Code] = MAD,
        [MDL.Code] = MDL,
        [MGA.Code] = MGA,
        [MKD.Code] = MKD,
        [MMK.Code] = MMK,
        [MNT.Code] = MNT,
        [MOP.Code] = MOP,
        [MRU.Code] = MRU,
        [MUR.Code] = MUR,
        [MVR.Code] = MVR,
        [MWK.Code] = MWK,
        [MXN.Code] = MXN,
        [MYR.Code] = MYR,
        [MZN.Code] = MZN,

        [NAD.Code] = NAD,
        [NGN.Code] = NGN,
        [NIO.Code] = NIO,
        [NOK.Code] = NOK,
        [NPR.Code] = NPR,
        [NZD.Code] = NZD,

        [OMR.Code] = OMR,

        [PAB.Code] = PAB,
        [PEN.Code] = PEN,
        [PGK.Code] = PGK,
        [PHP.Code] = PHP,
        [PKR.Code] = PKR,
        [PLN.Code] = PLN,
        [PYG.Code] = PYG,

        [QAR.Code] = QAR,

        [RON.Code] = RON,
        [RSD.Code] = RSD,
        [RUB.Code] = RUB,
        [RWF.Code] = RWF,

        [SAR.Code] = SAR,
        [SBD.Code] = SBD,
        [SCR.Code] = SCR,
        [SDG.Code] = SDG,
        [SEK.Code] = SEK,
        [SGD.Code] = SGD,
        [SHP.Code] = SHP,
        [SLE.Code] = SLE,
        [SOS.Code] = SOS,
        [SRD.Code] = SRD,
        [SSP.Code] = SSP,
        [STN.Code] = STN,
        [SYP.Code] = SYP,
        [SZL.Code] = SZL,

        [THB.Code] = THB,
        [TJS.Code] = TJS,
        [TMT.Code] = TMT,
        [TND.Code] = TND,
        [TOP.Code] = TOP,
        [TRY.Code] = TRY,
        [TTD.Code] = TTD,
        [TWD.Code] = TWD,
        [TZS.Code] = TZS,

        [UAH.Code] = UAH,
        [UGX.Code] = UGX,
        [USD.Code] = USD,
        [UYU.Code] = UYU,
        [UZS.Code] = UZS,

        [VES.Code] = VES,
        [VND.Code] = VND,
        [VUV.Code] = VUV,

        [WST.Code] = WST,

        [XAF.Code] = XAF,
        [XCD.Code] = XCD,
        [XOF.Code] = XOF,
        [XPF.Code] = XPF,

        [YER.Code] = YER,

        [ZAR.Code] = ZAR,
        [ZMW.Code] = ZMW,
        [ZWL.Code] = ZWL,
    };

    // -------------------------------------------------------------------------
    // Public API
    // -------------------------------------------------------------------------

    /// <summary>
    /// Returns the <see cref="Currency"/> for the given ISO 4217 alpha-3 code.
    /// The lookup is case-insensitive and trims surrounding whitespace.
    /// Throws <see cref="ArgumentNullException"/> for null input and
    /// <see cref="ArgumentException"/> for unrecognised codes.
    /// </summary>
    public static Currency From(string code)
    {
        string upper = code?.Trim().ToUpperInvariant()
            ?? throw new ArgumentNullException(nameof(code));

        return _all.TryGetValue(upper, out Currency? currency)
            ? currency
            : throw new ArgumentException($"Unsupported currency code: '{code}'", nameof(code));
    }

    /// <summary>
    /// Returns true when <paramref name="code"/> maps to a supported ISO 4217 currency.
    /// </summary>
    public static bool IsSupported(string code)
    {
        return !string.IsNullOrWhiteSpace(code)
            && _all.ContainsKey(code.Trim().ToUpperInvariant());
    }

    /// <summary>
    /// Returns a read-only view of all supported currencies.
    /// </summary>
    public static IReadOnlyDictionary<string, Currency> All => _all;

    // -------------------------------------------------------------------------
    // Equality — based on code only (two EUR instances are the same currency)
    // -------------------------------------------------------------------------

    public override bool Equals(object? obj)
    {
        return obj is Currency other && Code == other.Code;
    }

    public override int GetHashCode() => Code.GetHashCode();

    public static bool operator ==(Currency? a, Currency? b) => a?.Code == b?.Code;

    public static bool operator !=(Currency? a, Currency? b) => !(a == b);

    public override string ToString() => Code;
}