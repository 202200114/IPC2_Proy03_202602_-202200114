namespace Proyecto3.API.Models;

public class FacturaRegistro
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
    
    /* DateTime es un TDA propio de C# que se utiliza para representar un instante especifico
en el tiempo, incluye fecha y horas-minutos-segundos también puede incluir fracciones de segundos (como nanosegundos)*/
    public DateTime FechaFactura { get; set; }
    public DateTime FechaProcesamiento { get; set; }
    public bool Aprobada { get; set; }
    public string? CodigoAutorizacion { get; set; }
    //Se utilizan las estructuras propias del lenguaje en este proyecto como se puede ver al usar una List
    public List<string> Errores { get; set; } = new();
}
