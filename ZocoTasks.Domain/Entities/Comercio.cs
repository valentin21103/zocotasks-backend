using ZocoTasks.Domain.Common;
using ZocoTasks.Domain.Enums;

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
public class Comercio : EntidadBase, IAuditable, ISoftDelete
{
    public string NombreComercial { get; set; } = null!;

    /// <summary>Once digitos, sin guiones. Validado por modulo 11 en Business.</summary>
    public string Cuit { get; set; } = null!;

    public string NombreContacto { get; set; } = null!;

    public string? Telefono { get; set; }

    /// <summary>Se persiste como <c>citext</c> (comparacion case-insensitive).</summary>
    public string? Email { get; set; }

    public int RubroId { get; set; }
    public Rubro Rubro { get; set; } = null!;

    /// <summary>
    /// FK a <c>estado_comercio</c> tipada con el enum. Solo se modifica via
    /// <see cref="CambiarEstado"/>: el setter publico existe unicamente para que
    /// EF pueda materializar la entidad.
    /// </summary>
    public EstadoComercioEnum Estado { get; set; } = EstadoComercioEnum.Nuevo;
    public EstadoComercio EstadoNavegacion { get; set; } = null!;

    public int? UsuarioAsignadoId { get; set; }
    public Usuario? UsuarioAsignado { get; set; }

    public string? Notas { get; set; }

    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }
    public DateTime? FechaEliminacion { get; set; }

    /// <summary>
    /// Token de concurrencia optimista. Mapea la columna de sistema <c>xmin</c>
    /// de PostgreSQL, que cambia sola en cada UPDATE: no hace falta mantener una
    /// columna de version propia. Viaja al cliente como ETag.
    /// </summary>
    public uint Version { get; set; }

    public ICollection<Interaccion> Interacciones { get; set; } = new List<Interaccion>();
    public ICollection<HistorialEstado> Historial { get; set; } = new List<HistorialEstado>();
    public ICollection<AnalisisOportunidad> Analisis { get; set; } = new List<AnalisisOportunidad>();

    /// <summary>
    /// Deja asentado el estado inicial en el historial. Se llama una sola vez,
    /// al dar de alta el comercio: es el unico registro con estado anterior nulo.
    /// </summary>
    public HistorialEstado RegistrarEstadoInicial(int? usuarioId, DateTime? fecha = null)
    {
        var registro = new HistorialEstado
        {
            Comercio = this,
            EstadoAnterior = null,
            EstadoNuevo = Estado,
            UsuarioId = usuarioId,
            Motivo = "Alta del comercio",
            Fecha = fecha ?? DateTime.UtcNow
        };

        Historial.Add(registro);
        return registro;
    }

    /// <summary>
    /// Mueve el comercio en el pipeline dejando la traza en el historial.
    /// Unico camino para cambiar de estado: valida contra
    /// <see cref="MaquinaEstadoComercio"/> antes de mutar, de modo que un estado
    /// invalido no puede llegar a persistirse por ninguna via.
    /// </summary>
    /// <exception cref="Exceptions.EstadoTransicionInvalidaException">
    /// Si el pipeline no permite la transicion desde el estado actual.
    /// </exception>
    public HistorialEstado CambiarEstado(
        EstadoComercioEnum nuevoEstado,
        int? usuarioId,
        string? motivo = null,
        DateTime? fecha = null)
    {
        MaquinaEstadoComercio.ValidarTransicion(Estado, nuevoEstado);

        var registro = new HistorialEstado
        {
            Comercio = this,
            EstadoAnterior = Estado,
            EstadoNuevo = nuevoEstado,
            UsuarioId = usuarioId,
            Motivo = motivo,
            Fecha = fecha ?? DateTime.UtcNow
        };

        Estado = nuevoEstado;
        Historial.Add(registro);
        return registro;
    }
}
