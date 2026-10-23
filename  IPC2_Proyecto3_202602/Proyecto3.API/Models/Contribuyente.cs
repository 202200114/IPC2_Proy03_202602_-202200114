using System.Text.Json.Serialization;

namespace Proyecto3.API.Models;

/* Originalmente se tenía las clases con la keyword sealed, esto significaba que las clases no podian ser heredadas
 
Es recomendable utilizar sealed en clases específicas cuando no se desea permitir que otras clases hereden de ellas, ya
que al heredarse, podrian provocar curiosos incovenientes, aunque heredar sea un pilar de POO, no siempre es
recomendable usar herencia porque pueden llegar a darse problemas.
Sealed impide que las clases hijas alteren su comportamiento mediante la sobrescritura (override) de métodos heredados. 

Aun así tampoco hay que abusar del sealed
*/


public class Contribuyente
{
    [JsonRequired]
    public string NIT { get; set; } = string.Empty;

    [JsonRequired]
    public string Nombre { get; set; } = string.Empty;
}
