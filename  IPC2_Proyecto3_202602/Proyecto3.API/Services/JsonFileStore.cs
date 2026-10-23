using System.Text.Json;

namespace Proyecto3.API.Services;

/// <summary>Persistencia sencilla en archivos JSON locales, tal como permite el enunciado.</summary>
/// Igualmente cuenta con sealed
public sealed class JsonFileStore
{
    private readonly string _dataPath;
    private readonly SemaphoreSlim _semaforo = new(1, 1);
    private readonly JsonSerializerOptions _opciones = new(JsonSerializerDefaults.Web)
    {
        WriteIndented = true,
        PropertyNameCaseInsensitive = true
    };

    public JsonFileStore(IWebHostEnvironment env)
    {
        _dataPath = Path.Combine(env.ContentRootPath, "Data");
        Directory.CreateDirectory(_dataPath);
    }

    private string Ruta(string archivo) => Path.Combine(_dataPath, archivo);

    public async Task<List<T>> LeerListaAsync<T>(string archivo)
    {
        await _semaforo.WaitAsync();
        try
        {
            var ruta = Ruta(archivo);
            if (!File.Exists(ruta)) return new List<T>();
            var json = await File.ReadAllTextAsync(ruta);
            if (string.IsNullOrWhiteSpace(json))
                throw new InvalidDataException($"El archivo de datos '{ruta}' está vacío.");

            return JsonSerializer.Deserialize<List<T>>(json, _opciones)
                ?? throw new InvalidDataException($"El archivo de datos '{ruta}' contiene null en lugar de una lista.");
        }
        catch (JsonException ex)
        {
            throw new InvalidDataException($"El archivo de datos '{Ruta(archivo)}' contiene JSON inválido.", ex);
        }
        finally { _semaforo.Release(); }
    }

    public async Task GuardarListaAsync<T>(string archivo, List<T> datos)
    {
        await _semaforo.WaitAsync();
        try
        {
            var json = JsonSerializer.Serialize(datos, _opciones);
            var rutaTemporal = Ruta(archivo + ".tmp");
            await File.WriteAllTextAsync(rutaTemporal, json);
            File.Move(rutaTemporal, Ruta(archivo), true);
        }
        finally { _semaforo.Release(); }
    }

    public async Task InicializarAsync()
    {
        await GuardarListaAsync<Proyecto3.API.Models.Contribuyente>("contribuyentes.json", new());
        await GuardarListaAsync<Proyecto3.API.Models.FacturaRegistro>("facturas.json", new());
    }
}
