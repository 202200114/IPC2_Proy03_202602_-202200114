using System.Text.Json.Serialization;

namespace Proyecto3.API.Models;

public sealed class FacturaSolicitud
{
    [JsonRequired]
    [JsonPropertyName("TIEMPO")]
    public string TIEMPO { get; set; } = string.Empty;

    [JsonRequired]
    [JsonPropertyName("REFERENCIA")]
    public string REFERENCIA { get; set; } = string.Empty;

    [JsonRequired]
    [JsonPropertyName("NIT_EMISOR")]
    public string NIT_EMISOR { get; set; } = string.Empty;

    [JsonRequired]
    [JsonPropertyName("NIT_RECEPTOR")]
    public string NIT_RECEPTOR { get; set; } = string.Empty;

    [JsonRequired]
    [JsonPropertyName("VALOR")]
    public decimal VALOR { get; set; }

    [JsonRequired]
    [JsonPropertyName("IVA")]
    public decimal IVA { get; set; }

    [JsonRequired]
    [JsonPropertyName("TOTAL")]
    public decimal TOTAL { get; set; }
}
