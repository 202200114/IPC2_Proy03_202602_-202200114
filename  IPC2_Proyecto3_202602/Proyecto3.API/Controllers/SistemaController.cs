using Microsoft.AspNetCore.Mvc;
using Proyecto3.API.Services;

namespace Proyecto3.API.Controllers;

[ApiController]
[Route("api/sistema")]
public class SistemaController : ControllerBase
{
    private readonly FacturaService _service;
    public SistemaController(FacturaService service) => _service = service;

    [HttpPost("inicializar")]
    public async Task<IActionResult> Inicializar()
    {
        await _service.InicializarAsync();
        return Ok(new { mensaje = "Sistema inicializado correctamente." });
    }
}
