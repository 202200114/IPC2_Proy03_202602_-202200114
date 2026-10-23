using System.Globalization;
using System.Text.RegularExpressions;
using Proyecto3.API.Models;

namespace Proyecto3.API.Services;
//Esta clase también cuenta con sealed, por conveniencia se dejará, después de todo es una especifica
public sealed class FacturaService
{
    private const string ArchivoContribuyentes = "contribuyentes.json";
    private const string ArchivoFacturas = "facturas.json";
    private readonly JsonFileStore _store;
    private readonly SemaphoreSlim _procesamiento = new(1, 1);
    //Uso del regex
    private static readonly Regex RegexTiempo = new(
        @"(?<fecha>\d{1,2}/\d{1,2}/\d{4})\s+(?<hora>\d{1,2}:\d{1,2})",
        RegexOptions.Compiled | RegexOptions.CultureInvariant);
    private static readonly TimeZoneInfo ZonaHorariaGuatemala = TimeZoneInfo.FindSystemTimeZoneById(
        OperatingSystem.IsWindows() ? "Central America Standard Time" : "America/Guatemala");

    public FacturaService(JsonFileStore store) => _store = store;

    public async Task<RespuestaRtu> ActualizarRtuAsync(List<Contribuyente>? entradas)
    {
        var respuesta = new RespuestaRtu();
        if (entradas is null) return respuesta;

        await _procesamiento.WaitAsync();
        try
        {
            var guardados = await _store.LeerListaAsync<Contribuyente>(ArchivoContribuyentes);
            foreach (var entrada in entradas)
            {
                if (entrada is null)
                {
                    respuesta.NITS_INVALIDOS++;
                    continue;
                }

                var nit = NitValidator.Normalizar(entrada.NIT);
                if (!NitValidator.EsValido(nit))
                {
                    respuesta.NITS_INVALIDOS++;
                    continue;
                }

                var existente = guardados.FirstOrDefault(c => NitValidator.Normalizar(c.NIT) == nit);
                if (existente is null)
                {
                    guardados.Add(new Contribuyente { NIT = nit, Nombre = (entrada.Nombre ?? string.Empty).Trim() });
                    respuesta.CONTRIBUYENTES_NUEVOS++;
                }
                else
                {
                    existente.Nombre = (entrada.Nombre ?? string.Empty).Trim();
                    respuesta.CONTRIBUYENTES_ACTUALIZADOS++;
                }
            }
            await _store.GuardarListaAsync(ArchivoContribuyentes, guardados);
            return respuesta;
        }
        finally { _procesamiento.Release(); }
    }

    public async Task<RespuestaAutorizaciones> ProcesarAsync(List<FacturaSolicitud>? entradas)
    {
        var respuesta = new RespuestaAutorizaciones();
        if (entradas is null) return respuesta;

        await _procesamiento.WaitAsync();
        try
        {
            var contribuyentes = await _store.LeerListaAsync<Contribuyente>(ArchivoContribuyentes);
            var facturas = await _store.LeerListaAsync<FacturaRegistro>(ArchivoFacturas);
            var referencias = new HashSet<string>(facturas.Select(f => f.Referencia), StringComparer.Ordinal);
            var nitsExistentes = contribuyentes
                .Select(c => NitValidator.Normalizar(c.NIT))
                .ToHashSet(StringComparer.Ordinal);
            var fechaProcesamiento = TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, ZonaHorariaGuatemala);
            var prefijoDia = fechaProcesamiento.ToString("yyyyMMdd", CultureInfo.InvariantCulture);
            long correlativo = facturas
                .Where(f => f.Aprobada && f.CodigoAutorizacion is not null && f.CodigoAutorizacion.StartsWith(prefijoDia, StringComparison.Ordinal))
                .Select(f => f.CodigoAutorizacion is { Length: >= 8 } codigo &&
                    long.TryParse(codigo[^8..], out var n) ? n : 0L)
                .DefaultIfEmpty(0L).Max();

            foreach (var entrada in entradas)
            {
                var registro = new FacturaRegistro
                {
                    TIEMPOOriginal = entrada.TIEMPO ?? string.Empty,
                    Referencia = (entrada.REFERENCIA ?? string.Empty).Trim(),
                    NitEmisor = NitValidator.Normalizar(entrada.NIT_EMISOR),
                    NitReceptor = NitValidator.Normalizar(entrada.NIT_RECEPTOR),
                    Valor = entrada.VALOR,
                    IvaRecibido = entrada.IVA,
                    TotalRecibido = entrada.TOTAL,
                    FechaProcesamiento = fechaProcesamiento
                };

                var coincidencia = RegexTiempo.Match(registro.TIEMPOOriginal);
                if (!coincidencia.Success || !DateTime.TryParseExact(
                        coincidencia.Groups["fecha"].Value + " " + coincidencia.Groups["hora"].Value,
                        "d/M/yyyy H:m", CultureInfo.InvariantCulture, DateTimeStyles.None, out var fechaFactura))
                {
                    respuesta.resultadoAutorizaciones.cantidadDescartadasPorFormato++;
                    continue;
                }
                registro.FechaFactura = fechaFactura;

                if (string.IsNullOrWhiteSpace(registro.Referencia) || registro.Referencia.Length > 40)
                    registro.Errores.Add("REFERENCIA_INVALIDA");
                else if (!referencias.Add(registro.Referencia)) registro.Errores.Add("REFERENCIA_DUPLICADA");

                var emisorValido = NitValidator.EsValido(registro.NitEmisor);
                var receptorValido = NitValidator.EsValido(registro.NitReceptor);
                if (!emisorValido) registro.Errores.Add("NIT_EMISOR_INVALIDO");
                if (!receptorValido) registro.Errores.Add("NIT_RECEPTOR_INVALIDO");

                if (emisorValido && !nitsExistentes.Contains(registro.NitEmisor)) registro.Errores.Add("NIT_EMISOR_INEXISTENTE");
                if (receptorValido && !nitsExistentes.Contains(registro.NitReceptor)) registro.Errores.Add("NIT_RECEPTOR_INEXISTENTE");

                if (registro.Valor <= 0 || decimal.Round(registro.Valor, 2) != registro.Valor)
                    registro.Errores.Add("VALOR_INVALIDO");

                registro.IvaCalculado = decimal.Round(registro.Valor * 0.12m, 2, MidpointRounding.AwayFromZero);
                registro.TotalCalculado = registro.Valor + registro.IvaCalculado;
                if (registro.IvaRecibido != registro.IvaCalculado) registro.Errores.Add("IVA_MAL_CALCULADO");
                if (registro.TotalRecibido != registro.Valor + registro.IvaRecibido) registro.Errores.Add("TOTAL_MAL_CALCULADO");

                registro.Errores = registro.Errores.Distinct(StringComparer.Ordinal).ToList();
                registro.Aprobada = registro.Errores.Count == 0;
                if (registro.Aprobada)
                {
                    correlativo++;
                    registro.CodigoAutorizacion = prefijoDia + correlativo.ToString("D8", CultureInfo.InvariantCulture);
                    respuesta.resultadoAutorizaciones.cantidadAutorizacionesAprobadas++;
                    respuesta.resultadoAutorizaciones.detalleAprobaciones.Add(new DetalleAprobacion
                    {
                        referencia = registro.Referencia,
                        codigoAprobacion = long.Parse(registro.CodigoAutorizacion, CultureInfo.InvariantCulture)
                    });
                }
                if (registro.Errores.Contains("NIT_EMISOR_INEXISTENTE"))
                    respuesta.resultadoAutorizaciones.cantidadAutorizacionesRechazadasPorNitEmisorInexistente++;
                if (registro.Errores.Contains("NIT_RECEPTOR_INEXISTENTE"))
                    respuesta.resultadoAutorizaciones.cantidadAutorizacionesRechazadasPorNitReceptorInexistente++;
                if (registro.Errores.Contains("NIT_EMISOR_INVALIDO"))
                    respuesta.resultadoAutorizaciones.cantidadAutorizacionesRechazadasPorNitEmisorInvalido++;
                if (registro.Errores.Contains("NIT_RECEPTOR_INVALIDO"))
                    respuesta.resultadoAutorizaciones.cantidadAutorizacionesRechazadasPorNitReceptorInvalido++;
                if (registro.Errores.Contains("IVA_MAL_CALCULADO"))
                    respuesta.resultadoAutorizaciones.cantidadAutorizacionesRechazadasPorIvaMalCalculado++;
                if (registro.Errores.Contains("TOTAL_MAL_CALCULADO"))
                    respuesta.resultadoAutorizaciones.cantidadAutorizacionesRechazadasPorTotalMalCalculado++;
                if (registro.Errores.Contains("REFERENCIA_DUPLICADA"))
                    respuesta.resultadoAutorizaciones.cantidadAutorizacionesRechazadasPorReferenciaDuplicada++;
                if (!registro.Aprobada)
                    respuesta.resultadoAutorizaciones.cantidadAutorizacionesRechazadas++;

                facturas.Add(registro);
            }

            await _store.GuardarListaAsync(ArchivoFacturas, facturas);
            return respuesta;
        }
        finally { _procesamiento.Release(); }
    }

    public Task<List<Contribuyente>> ObtenerContribuyentesAsync() =>
        _store.LeerListaAsync<Contribuyente>(ArchivoContribuyentes);

    public Task<List<FacturaRegistro>> ObtenerFacturasAsync() =>
        _store.LeerListaAsync<FacturaRegistro>(ArchivoFacturas);

    public async Task InicializarAsync()
    {
        await _procesamiento.WaitAsync();
        try { await _store.InicializarAsync(); }
        finally { _procesamiento.Release(); }
    }
}
