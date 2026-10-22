namespace Proyecto3.API.Models;

public sealed class FacturaRegistro
{
    public string TIEMPOOriginal { get; set; } = string.Empty;
    public string Referencia { get; set; } = string.Empty;
    public string NitEmisor { get; set; } = string.Empty;
    public string NitReceptor { get; set; } = string.Empty;
    public decimal Valor { get; set; }
    public decimal IvaRecibido { get; set; }
    public decimal IvaCalculado { get; set; }
    public decimal TotalRecibido { get; set; }
    public decimal TotalCalculado { get; set; }
    public DateTime FechaFactura { get; set; }
    public DateTime FechaProcesamiento { get; set; }
    public bool Aprobada { get; set; }
    public string? CodigoAutorizacion { get; set; }
    public List<string> Errores { get; set; } = new();
}
