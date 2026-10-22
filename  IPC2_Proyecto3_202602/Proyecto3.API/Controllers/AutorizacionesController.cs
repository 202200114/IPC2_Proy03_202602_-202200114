using Microsoft.AspNetCore.Mvc;
using Proyecto3.API.Models;
using Proyecto3.API.Services;

namespace Proyecto3.API.Controllers;

[ApiController]
[Route("api/autorizaciones")]
public sealed class AutorizacionesController : ControllerBase
{
    private readonly FacturaService _service;
    public AutorizacionesController(FacturaService service) => _service = service;

    [HttpPost("procesar")]
    public async Task<ActionResult<RespuestaAutorizaciones>> Procesar([FromBody] List<FacturaSolicitud?>? facturas)
    {
        if (facturas is null || facturas.Count == 0)
            return BadRequest(new { error = "El mensaje debe incluir al menos una solicitud de autorización." });

        var solicitudes = facturas.OfType<FacturaSolicitud>().ToList();
        if (solicitudes.Count != facturas.Count)
            return BadRequest(new { error = "El arreglo no puede contener solicitudes nulas." });

        var respuesta = await _service.ProcesarAsync(solicitudes);
        return Ok(respuesta);
    }
}
