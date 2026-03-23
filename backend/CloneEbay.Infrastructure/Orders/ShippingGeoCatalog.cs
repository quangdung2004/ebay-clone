namespace CloneEbay.Infrastructure.Orders;

public static class ShippingGeoCatalog
{
    public const string DefaultCountryCode = "VN";
    public const string DefaultContinent = "ASIA";

    public static (string CountryCode, string Continent) Resolve(string? country)
    {
        var normalized = NormalizeCountry(country);

        return normalized switch
        {
            "VN" => ("VN", "ASIA"),
            "JP" => ("JP", "ASIA"),
            "CN" => ("CN", "ASIA"),
            "KR" => ("KR", "ASIA"),
            "TH" => ("TH", "ASIA"),
            "SG" => ("SG", "ASIA"),
            "MY" => ("MY", "ASIA"),
            "ID" => ("ID", "ASIA"),
            "PH" => ("PH", "ASIA"),
            "IN" => ("IN", "ASIA"),
            "AE" => ("AE", "ASIA"),
            "SA" => ("SA", "ASIA"),

            "GB" => ("GB", "EUROPE"),
            "FR" => ("FR", "EUROPE"),
            "DE" => ("DE", "EUROPE"),
            "IT" => ("IT", "EUROPE"),
            "ES" => ("ES", "EUROPE"),
            "NL" => ("NL", "EUROPE"),
            "BE" => ("BE", "EUROPE"),
            "SE" => ("SE", "EUROPE"),
            "NO" => ("NO", "EUROPE"),
            "DK" => ("DK", "EUROPE"),
            "CH" => ("CH", "EUROPE"),
            "PL" => ("PL", "EUROPE"),
            "PT" => ("PT", "EUROPE"),
            "IE" => ("IE", "EUROPE"),

            "US" => ("US", "NORTH_AMERICA"),
            "CA" => ("CA", "NORTH_AMERICA"),
            "MX" => ("MX", "NORTH_AMERICA"),

            "BR" => ("BR", "SOUTH_AMERICA"),
            "AR" => ("AR", "SOUTH_AMERICA"),
            "CL" => ("CL", "SOUTH_AMERICA"),
            "CO" => ("CO", "SOUTH_AMERICA"),
            "PE" => ("PE", "SOUTH_AMERICA"),

            "ZA" => ("ZA", "AFRICA"),
            "EG" => ("EG", "AFRICA"),
            "NG" => ("NG", "AFRICA"),
            "KE" => ("KE", "AFRICA"),
            "MA" => ("MA", "AFRICA"),

            "AU" => ("AU", "OCEANIA"),
            "NZ" => ("NZ", "OCEANIA"),

            _ => (DefaultCountryCode, DefaultContinent)
        };
    }

    private static string NormalizeCountry(string? country)
    {
        var value = (country ?? string.Empty).Trim().ToUpperInvariant();

        return value switch
        {
            "" => "VN",

            "VN" => "VN",
            "VIETNAM" => "VN",
            "VIỆT NAM" => "VN",

            "US" => "US",
            "USA" => "US",
            "UNITED STATES" => "US",
            "UNITED STATES OF AMERICA" => "US",

            "UK" => "GB",
            "GB" => "GB",
            "GREAT BRITAIN" => "GB",
            "UNITED KINGDOM" => "GB",
            "ENGLAND" => "GB",

            "JP" => "JP",
            "JAPAN" => "JP",

            "CN" => "CN",
            "CHINA" => "CN",

            "KR" => "KR",
            "KOREA" => "KR",
            "SOUTH KOREA" => "KR",
            "REPUBLIC OF KOREA" => "KR",

            "TH" => "TH",
            "THAILAND" => "TH",

            "SG" => "SG",
            "SINGAPORE" => "SG",

            "MY" => "MY",
            "MALAYSIA" => "MY",

            "ID" => "ID",
            "INDONESIA" => "ID",

            "PH" => "PH",
            "PHILIPPINES" => "PH",

            "IN" => "IN",
            "INDIA" => "IN",

            "AE" => "AE",
            "UAE" => "AE",
            "UNITED ARAB EMIRATES" => "AE",

            "SA" => "SA",
            "SAUDI ARABIA" => "SA",

            "FR" => "FR",
            "FRANCE" => "FR",

            "DE" => "DE",
            "GERMANY" => "DE",

            "IT" => "IT",
            "ITALY" => "IT",

            "ES" => "ES",
            "SPAIN" => "ES",

            "NL" => "NL",
            "NETHERLANDS" => "NL",

            "BE" => "BE",
            "BELGIUM" => "BE",

            "SE" => "SE",
            "SWEDEN" => "SE",

            "NO" => "NO",
            "NORWAY" => "NO",

            "DK" => "DK",
            "DENMARK" => "DK",

            "CH" => "CH",
            "SWITZERLAND" => "CH",

            "PL" => "PL",
            "POLAND" => "PL",

            "PT" => "PT",
            "PORTUGAL" => "PT",

            "IE" => "IE",
            "IRELAND" => "IE",

            "CA" => "CA",
            "CANADA" => "CA",

            "MX" => "MX",
            "MEXICO" => "MX",

            "BR" => "BR",
            "BRAZIL" => "BR",

            "AR" => "AR",
            "ARGENTINA" => "AR",

            "CL" => "CL",
            "CHILE" => "CL",

            "CO" => "CO",
            "COLOMBIA" => "CO",

            "PE" => "PE",
            "PERU" => "PE",

            "ZA" => "ZA",
            "SOUTH AFRICA" => "ZA",

            "EG" => "EG",
            "EGYPT" => "EG",

            "NG" => "NG",
            "NIGERIA" => "NG",

            "KE" => "KE",
            "KENYA" => "KE",

            "MA" => "MA",
            "MOROCCO" => "MA",

            "AU" => "AU",
            "AUSTRALIA" => "AU",

            "NZ" => "NZ",
            "NEW ZEALAND" => "NZ",

            _ => value
        };
    }
}