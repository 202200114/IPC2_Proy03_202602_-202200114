using Microsoft.AspNetCore.Mvc;
using Proyecto3.API.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers();
builder.Services.Configure<ApiBehaviorOptions>(options =>
{
    options.InvalidModelStateResponseFactory = context =>
    {
        var errores = context.ModelState.Values
            .SelectMany(estado => estado.Errors)
            .Select(error => string.IsNullOrWhiteSpace(error.ErrorMessage)
                ? "El JSON no coincide con la estructura esperada."
                : error.ErrorMessage);
        return new BadRequestObjectResult(new { error = string.Join(" ", errores) });
    };
});
builder.Services.AddSingleton<JsonFileStore>();
builder.Services.AddSingleton<FacturaService>();

var app = builder.Build();

// Se deja HTTP en desarrollo para que coincida con el perfil local de Rider.
// En caso de configurar HTTPS correctamente, puede habilitarse UseHttpsRedirection().
app.MapGet("/", () => Results.Ok(new
{
    servicio = "Proyecto3.API",
    estado = "funcionando",
    endpoints = new[]
    {
        "POST /api/rtu/actualizar",
        "POST /api/autorizaciones/procesar",
        "GET /api/consultas/contribuyente?nit=...&tipo=emitidas&desde=2025-01-01&hasta=2025-01-31",
        "GET /api/consultas/resumen-iva?fecha=2025-01-15",
        "GET /api/consultas/resumen-rango?desde=2025-01-01&hasta=2025-01-31&conIva=true",
        "GET /api/consultas/estadisticas?desde=2025-01-01&hasta=2025-01-31",
        "POST /api/sistema/inicializar"
    }
}));
app.MapControllers();
app.Run();
