using Microsoft.AspNetCore.Mvc;
using Proyecto3.API.Models;
using Proyecto3.API.Services;

namespace Proyecto3.API.Controllers;

[ApiController]
[Route("api/rtu")]
public class RtuController : ControllerBase
{
    private readonly FacturaService _service;
    public RtuController(FacturaService service) => _service = service;

    [HttpPost("actualizar")]
    public async Task<ActionResult<RespuestaRtu>> Actualizar([FromBody] List<Contribuyente?>? contribuyentes)
    {
        if (contribuyentes is null || contribuyentes.Count == 0)
            return BadRequest(new { error = "El mensaje debe incluir al menos un contribuyente." });

        var entradas = contribuyentes.OfType<Contribuyente>().ToList();
        if (entradas.Count != contribuyentes.Count)
            return BadRequest(new { error = "El arreglo no puede contener contribuyentes nulos." });

        var respuesta = await _service.ActualizarRtuAsync(entradas);
        return Ok(respuesta);
    }
}
