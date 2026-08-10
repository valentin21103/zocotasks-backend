using ZocoTasks.Domain.Common;
using ZocoTasks.Domain.Enums;
using ZocoTasks.Domain.Exceptions;

namespace ZocoTasks.Domain.Entities;

/// <summary>
/// Comercio en seguimiento comercial. Entidad principal del sistema.
/// </summary>
/// <remarks>
/// Dos columnas de la tabla no aparecen acá a proposito:
/// <list type="bullet">
/// <item><c>search_vector</c> (tsvector) se declara como shadow property en la
/// configuracion de EF, porque su tipo CLR pertenece a Npgsql y Domain no
/// referencia paquetes.</item>
/// <item><c>xmin</c> si aparece, como <see cref="Version"/>, porque
/// <see cref="uint"/> es un tipo de la BCL y el servicio necesita leer el valor
/// para emitir el ETag.</item>
/// </list>
/// </remarks>
public class Comercio : ISoftDelete
{
    public int Id { get; set; }

    public string NombreComercial { get; set; } = null!;

    /// <summary>Once digitos, sin guiones.</summary>
    public string Cuit { get; set; } = null!;

    public string NombreContacto { get; set; } = null!;

    public string? Telefono { get; set; }

    public string? Email { get; set; }

    public int RubroId { get; set; }
    public Rubro Rubro { get; set; } = null!;


    public EstadoComercioEnum Estado { get; set; } = EstadoComercioEnum.Nuevo;
    public EstadoComercio EstadoNavegacion { get; set; } = null!;

    public int? UsuarioAsignadoId { get; set; }
    public Usuario? UsuarioAsignado { get; set; }

    public string? Notas { get; set; }

    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
    public DateTime? FechaEliminacion { get; set; }


    public uint Version { get; set; }

    public ICollection<Interaccion> Interacciones { get; set; } = new List<Interaccion>();

    /// <summary>
    /// Mueve el comercio en el pipeline. Unico camino para cambiar de estado.
    /// La unica transicion invalida es la de un estado a si mismo, porque no
    /// es un cambio.
    /// </summary>
    /// <exception cref="Exceptions.EstadoTransicionInvalidaException">
    /// Si se manda el mismo estado que el comercio ya tiene.
    /// </exception>
    public void CambiarEstado(EstadoComercioEnum nuevoEstado)
    {
        if (nuevoEstado == Estado)
        {
            throw new EstadoTransicionInvalidaException(Estado, nuevoEstado);
        }

        Estado = nuevoEstado;
    }
}
