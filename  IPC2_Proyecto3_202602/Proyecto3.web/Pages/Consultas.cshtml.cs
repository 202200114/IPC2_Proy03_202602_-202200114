using System.Net.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Proyecto3.Web.Pages;

public sealed class ConsultasModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    public ConsultasModel(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    [BindProperty] public string Nit { get; set; } = "";
    [BindProperty] public string Tipo { get; set; } = "emitidas";
    [BindProperty] public string Desde { get; set; } = DateTime.Today.AddDays(-30).ToString("yyyy-MM-dd");
    [BindProperty] public string Hasta { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");
    [BindProperty] public string Fecha { get; set; } = DateTime.Today.ToString("yyyy-MM-dd");
    [BindProperty] public bool ConIva { get; set; } = true;
    public string? TituloResultado { get; private set; }
    public string? Resultado { get; private set; }
    public string? Error { get; private set; }

    public async Task<IActionResult> OnPostContribuyenteAsync()
    {
        if (string.IsNullOrWhiteSpace(Nit)) return MostrarError("Escribe el NIT del contribuyente.");
        return await ConsultarAsync("Consulta de facturas por contribuyente", $"api/consultas/contribuyente?nit={Q(Nit)}&tipo={Q(Tipo)}&desde={Q(Desde)}&hasta={Q(Hasta)}");
    }

    public async Task<IActionResult> OnPostIvaAsync() =>
        await ConsultarAsync("Resumen de IVA por fecha y NIT", $"api/consultas/resumen-iva?fecha={Q(Fecha)}");

    public async Task<IActionResult> OnPostRangoAsync() =>
        await ConsultarAsync("Resumen por rango de fechas", $"api/consultas/resumen-rango?desde={Q(Desde)}&hasta={Q(Hasta)}&conIva={ConIva.ToString().ToLowerInvariant()}");

    public async Task<IActionResult> OnPostEstadisticasAsync() =>
        await ConsultarAsync("Estadísticas y errores por fecha", $"api/consultas/estadisticas?desde={Q(Desde)}&hasta={Q(Hasta)}");

    public async Task<IActionResult> OnPostInicializarAsync()
    {
        try
        {
            var cliente = _httpClientFactory.CreateClient("Proyecto3API");
            using var respuesta = await cliente.PostAsync("api/sistema/inicializar", null);
            Resultado = await respuesta.Content.ReadAsStringAsync();
            TituloResultado = "Inicialización del sistema";
            if (!respuesta.IsSuccessStatusCode) Error = "La API no pudo inicializar el sistema.";
        }
        catch (HttpRequestException ex) { Error = "No se pudo conectar con la API: " + ex.Message; }
        catch (OperationCanceledException ex) { Error = "La solicitud de inicialización excedió el tiempo límite: " + ex.Message; }
        return Page();
    }

    private async Task<IActionResult> ConsultarAsync(string titulo, string ruta)
    {
        try
        {
            var cliente = _httpClientFactory.CreateClient("Proyecto3API");
            using var respuesta = await cliente.GetAsync(ruta);
            Resultado = await respuesta.Content.ReadAsStringAsync();
            TituloResultado = titulo;
            if (!respuesta.IsSuccessStatusCode) Error = "La consulta devolvió un error HTTP " + (int)respuesta.StatusCode + ". Revisa las fechas y los parámetros.";
        }
        catch (HttpRequestException ex) { Error = "No se pudo conectar con la API: " + ex.Message; }
        catch (OperationCanceledException ex) { Error = "La consulta excedió el tiempo límite: " + ex.Message; }
        return Page();
    }

    private IActionResult MostrarError(string mensaje) { Error = mensaje; return Page(); }
    private static string Q(string valor) => Uri.EscapeDataString(valor ?? "");
}
