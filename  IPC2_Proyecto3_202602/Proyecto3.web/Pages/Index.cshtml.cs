using System.Net.Http.Headers;
using System.Text;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace Proyecto3.Web.Pages;

public sealed class IndexModel : PageModel
{
    private readonly IHttpClientFactory _httpClientFactory;
    public IndexModel(IHttpClientFactory httpClientFactory) => _httpClientFactory = httpClientFactory;

    [BindProperty] public List<IFormFile> ArchivosJson { get; set; } = new();
    [BindProperty] public string TipoMensaje { get; set; } = "facturas";
    public string? Respuesta { get; private set; }
    public string? Error { get; private set; }

    public async Task<IActionResult> OnPostEnviarAsync()
    {
        if (ArchivosJson.Count == 0)
        {
            Error = "Selecciona al menos un archivo JSON antes de enviarlo.";
            return Page();
        }
        if (TipoMensaje is not ("facturas" or "rtu"))
        {
            Error = "Selecciona un tipo de solicitud válido.";
            return Page();
        }

        var cliente = _httpClientFactory.CreateClient("Proyecto3API");
        var respuestas = new List<string>();
        var errores = new List<string>();

        foreach (var archivo in ArchivosJson)
        {
            if (archivo.Length == 0)
            {
                errores.Add($"{archivo.FileName}: el archivo está vacío.");
                continue;
            }

            if (!Path.GetExtension(archivo.FileName).Equals(".json", StringComparison.OrdinalIgnoreCase))
            {
                errores.Add($"{archivo.FileName}: el archivo debe tener extensión .json.");
                continue;
            }

            try
            {
                using var lector = new StreamReader(archivo.OpenReadStream());
                var contenido = await lector.ReadToEndAsync();
                using var documento = System.Text.Json.JsonDocument.Parse(contenido);
                if (documento.RootElement.ValueKind != System.Text.Json.JsonValueKind.Array)
                {
                    errores.Add($"{archivo.FileName}: el JSON debe contener un arreglo de objetos.");
                    continue;
                }

                var tipoDetectado = DetectarTipoMensaje(documento.RootElement, TipoMensaje);
                var ruta = tipoDetectado == "rtu" ? "api/rtu/actualizar" : "api/autorizaciones/procesar";
                using var contenidoHttp = new StringContent(contenido, Encoding.UTF8, "application/json");
                using var respuestaHttp = await cliente.PostAsync(ruta, contenidoHttp);
                var cuerpo = await respuestaHttp.Content.ReadAsStringAsync();
                respuestas.Add($"{archivo.FileName} ({tipoDetectado})\nHTTP {(int)respuestaHttp.StatusCode} {respuestaHttp.ReasonPhrase}\n{cuerpo}");
                if (!respuestaHttp.IsSuccessStatusCode)
                    errores.Add($"{archivo.FileName}: la API rechazó la solicitud (HTTP {(int)respuestaHttp.StatusCode}).");
            }
            catch (System.Text.Json.JsonException)
            {
                errores.Add($"{archivo.FileName}: el archivo no contiene un JSON válido.");
            }
            catch (HttpRequestException ex)
            {
                errores.Add($"{archivo.FileName}: no se pudo conectar con la API: {ex.Message}");
            }
            catch (OperationCanceledException ex)
            {
                errores.Add($"{archivo.FileName}: la solicitud a la API excedió el tiempo límite: {ex.Message}");
            }
            catch (IOException ex)
            {
                errores.Add($"{archivo.FileName}: no se pudo leer el archivo: {ex.Message}");
            }
            catch (UnauthorizedAccessException ex)
            {
                errores.Add($"{archivo.FileName}: no se pudo acceder al archivo: {ex.Message}");
            }
        }

        Respuesta = respuestas.Count == 0 ? null : string.Join("\n\n", respuestas);
        Error = errores.Count == 0 ? null : string.Join("\n", errores);
        return Page();
    }

    private static string DetectarTipoMensaje(System.Text.Json.JsonElement arreglo, string tipoPredeterminado)
    {
        if (arreglo.GetArrayLength() == 0 || arreglo[0].ValueKind != System.Text.Json.JsonValueKind.Object)
            return tipoPredeterminado;

        var propiedades = arreglo[0].EnumerateObject()
            .Select(propiedad => propiedad.Name)
            .ToHashSet(StringComparer.OrdinalIgnoreCase);
        if (propiedades.Contains("NIT") && propiedades.Contains("nombre"))
            return "rtu";
        if (propiedades.Contains("TIEMPO") && propiedades.Contains("REFERENCIA") &&
            propiedades.Contains("NIT_EMISOR"))
            return "facturas";

        return tipoPredeterminado;
    }
}
