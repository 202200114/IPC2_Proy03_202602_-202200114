using System.Text.Json.Serialization;

namespace Proyecto3.API.Models;

public sealed class Contribuyente
{
    [JsonRequired]
    public string NIT { get; set; } = string.Empty;

    [JsonRequired]
    public string Nombre { get; set; } = string.Empty;
}
