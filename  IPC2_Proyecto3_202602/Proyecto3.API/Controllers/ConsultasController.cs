using Microsoft.AspNetCore.Mvc;
using Proyecto3.API.Models;
using Proyecto3.API.Services;

namespace Proyecto3.API.Controllers;

[ApiController]
[Route("api/consultas")]
public sealed class ConsultasController : ControllerBase
{
    private readonly FacturaService _service;
    public ConsultasController(FacturaService service) => _service = service;

    [HttpGet("contribuyente")]
    public async Task<IActionResult> PorContribuyente([FromQuery] string nit, [FromQuery] string tipo,
        [FromQuery] DateTime desde, [FromQuery] DateTime hasta)
    {
        if (!NitValidator.EsValido(nit))
            return BadRequest(new { error = "El NIT del contribuyente no es válido." });
        var tipoValido = string.Equals(tipo, "emitidas", StringComparison.OrdinalIgnoreCase) ||
            string.Equals(tipo, "recibidas", StringComparison.OrdinalIgnoreCase);
        if (!tipoValido)
            return BadRequest(new { error = "El tipo debe ser 'emitidas' o 'recibidas'." });
        if (desde.Date > hasta.Date) return BadRequest(new { error = "La fecha inicial debe ser menor o igual a la fecha final." });
        var normalizado = NitValidator.Normalizar(nit);
        var registros = await _service.ObtenerFacturasAsync();
        var filtradas = registros.Where(f => f.Aprobada && f.FechaFactura.Date >= desde.Date && f.FechaFactura.Date <= hasta.Date &&
            (tipo.Equals("emitidas", StringComparison.OrdinalIgnoreCase)
                ? f.NitEmisor == normalizado
                : tipo.Equals("recibidas", StringComparison.OrdinalIgnoreCase) && f.NitReceptor == normalizado)).ToList();
        var porFecha = filtradas.GroupBy(f => f.FechaFactura.Date)
            .OrderBy(grupo => grupo.Key)
            .Select(grupo => new
            {
                fecha = grupo.Key.ToString("dd/MM/yyyy"),
                cantidadFacturasAutorizadas = grupo.Count(),
                montoTotalFacturado = grupo.Sum(f => f.TotalCalculado)
            }).ToList();
        return Ok(new { nit = normalizado, tipo, detallePorFecha = porFecha, totalFacturadoRango = filtradas.Sum(f => f.TotalCalculado) });
    }

    [HttpGet("resumen-iva")]
    public async Task<IActionResult> ResumenIva([FromQuery] DateTime fecha)
    {
        var facturas = (await _service.ObtenerFacturasAsync()).Where(f => f.Aprobada && f.FechaFactura.Date == fecha.Date).ToList();
        var nits = facturas.SelectMany(f => new[] { f.NitEmisor, f.NitReceptor }).Distinct().OrderBy(n => n).ToList();
        var resumen = nits.Select(nit => new
        {
            nit,
            ivaEmitido = facturas.Where(f => f.NitEmisor == nit).Sum(f => f.IvaCalculado),
            ivaRecibido = facturas.Where(f => f.NitReceptor == nit).Sum(f => f.IvaCalculado)
        }).ToList();
        return Ok(new { fecha = fecha.ToString("dd/MM/yyyy"), resumen });
    }

    [HttpGet("resumen-rango")]
    public async Task<IActionResult> ResumenRango([FromQuery] DateTime desde, [FromQuery] DateTime hasta, [FromQuery] bool conIva = true)
    {
        if (desde.Date > hasta.Date) return BadRequest(new { error = "La fecha inicial debe ser menor o igual a la fecha final." });
        var facturas = (await _service.ObtenerFacturasAsync()).Where(f => f.Aprobada && f.FechaFactura.Date >= desde.Date && f.FechaFactura.Date <= hasta.Date).ToList();
        var detalle = facturas.GroupBy(f => f.FechaFactura.Date)
            .OrderBy(grupo => grupo.Key)
            .Select(grupo => new
            {
                fecha = grupo.Key.ToString("dd/MM/yyyy"),
                valorAutorizado = grupo.Sum(f => conIva ? f.TotalCalculado : f.Valor)
            }).ToList();
        return Ok(new { desde = desde.ToString("dd/MM/yyyy"), hasta = hasta.ToString("dd/MM/yyyy"), conIva, detalle, totalRango = detalle.Sum(d => d.valorAutorizado) });
    }

    [HttpGet("estadisticas")]
    public async Task<IActionResult> Estadisticas([FromQuery] DateTime desde, [FromQuery] DateTime hasta)
    {
        if (desde.Date > hasta.Date) return BadRequest(new { error = "La fecha inicial debe ser menor o igual a la fecha final." });
        var facturas = (await _service.ObtenerFacturasAsync()).Where(f => f.FechaFactura != default && f.FechaFactura.Date >= desde.Date && f.FechaFactura.Date <= hasta.Date).ToList();
        var salida = facturas.GroupBy(f => f.FechaFactura.Date)
            .OrderBy(grupo => grupo.Key)
            .Select(grupo =>
            {
                var g = grupo.ToList();
                return new
                {
                    fecha = grupo.Key.ToString("dd/MM/yyyy"),
                    cantidadTotalFacturasRecibidas = g.Count,
                    nitEmisorInvalido = g.Count(f => f.Errores.Contains("NIT_EMISOR_INVALIDO")),
                    nitReceptorInvalido = g.Count(f => f.Errores.Contains("NIT_RECEPTOR_INVALIDO")),
                    nitEmisorInexistente = g.Count(f => f.Errores.Contains("NIT_EMISOR_INEXISTENTE")),
                    nitReceptorInexistente = g.Count(f => f.Errores.Contains("NIT_RECEPTOR_INEXISTENTE")),
                    ivaMalCalculado = g.Count(f => f.Errores.Contains("IVA_MAL_CALCULADO")),
                    totalMalCalculado = g.Count(f => f.Errores.Contains("TOTAL_MAL_CALCULADO")),
                    referenciaDuplicada = g.Count(f => f.Errores.Contains("REFERENCIA_DUPLICADA")),
                    cantidadFacturasSinErrores = g.Count(f => f.Errores.Count == 0),
                    cantidadEmisoresDistintos = g.Select(f => f.NitEmisor).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().Count(),
                    cantidadReceptoresDistintos = g.Select(f => f.NitReceptor).Where(n => !string.IsNullOrWhiteSpace(n)).Distinct().Count()
                };
            }).ToList();
        return Ok(new { desde = desde.ToString("dd/MM/yyyy"), hasta = hasta.ToString("dd/MM/yyyy"), reporte = salida });
    }

    [HttpGet("facturas")]
    public async Task<IActionResult> Facturas([FromQuery] DateTime desde, [FromQuery] DateTime hasta)
    {
        if (desde.Date > hasta.Date) return BadRequest(new { error = "La fecha inicial debe ser menor o igual a la fecha final." });
        var registros = (await _service.ObtenerFacturasAsync()).Where(f => f.FechaFactura != default && f.FechaFactura.Date >= desde.Date && f.FechaFactura.Date <= hasta.Date).OrderBy(f => f.FechaFactura).ToList();
        return Ok(registros);
    }

    [HttpGet("contribuyentes")]
    public async Task<IActionResult> Contribuyentes() => Ok(await _service.ObtenerContribuyentesAsync());
}
