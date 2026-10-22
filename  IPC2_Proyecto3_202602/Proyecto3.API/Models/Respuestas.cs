using System.Text.Json.Serialization;

namespace Proyecto3.API.Models;

public sealed class RespuestaRtu
{
    [JsonPropertyName("CONTRIBUYENTES_NUEVOS")]
    public int CONTRIBUYENTES_NUEVOS { get; set; }
    [JsonPropertyName("CONTRIBUYENTES_ACTUALIZADOS")]
    public int CONTRIBUYENTES_ACTUALIZADOS { get; set; }
    [JsonPropertyName("NITS_INVALIDOS")]
    public int NITS_INVALIDOS { get; set; }
}

public sealed class RespuestaAutorizaciones
{
    public ResultadoAutorizaciones resultadoAutorizaciones { get; set; } = new();
}

public sealed class ResultadoAutorizaciones
{
    public int cantidadAutorizacionesAprobadas { get; set; }
    public int cantidadAutorizacionesRechazadasPorNitEmisorInexistente { get; set; }
    public int cantidadAutorizacionesRechazadasPorNitReceptorInexistente { get; set; }
    public int cantidadAutorizacionesRechazadasPorNitEmisorInvalido { get; set; }
    public int cantidadAutorizacionesRechazadasPorNitReceptorInvalido { get; set; }
    public int cantidadAutorizacionesRechazadasPorIvaMalCalculado { get; set; }
    public int cantidadAutorizacionesRechazadasPorTotalMalCalculado { get; set; }
    public int cantidadAutorizacionesRechazadasPorReferenciaDuplicada { get; set; }
    public int cantidadAutorizacionesRechazadas { get; set; }
    public int cantidadDescartadasPorFormato { get; set; }
    public List<DetalleAprobacion> detalleAprobaciones { get; set; } = new();
}

public sealed class DetalleAprobacion
{
    public string referencia { get; set; } = string.Empty;
    public long codigoAprobacion { get; set; }
}
