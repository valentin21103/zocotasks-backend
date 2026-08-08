using ZocoTasks.Domain.Enums;

namespace ZocoTasks.Business.DTOs;

/// <summary>
/// Fila del listado. Deliberadamente mas chico que el detalle: el listado no
/// necesita las notas completas ni las interacciones, y traerlas seria mover
/// datos de mas en cada pagina.
/// </summary>
public class ComercioListItemDto
{
    public int Id { get; set; }
    public string NombreComercial { get; set; } = string.Empty;
    public string Cuit { get; set; } = string.Empty;
    public string NombreContacto { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Email { get; set; }

    public int RubroId { get; set; }
    public string Rubro { get; set; } = string.Empty;

    public EstadoComercioEnum Estado { get; set; }
    public string EstadoNombre { get; set; } = string.Empty;

    public DateTime FechaCreacion { get; set; }
    public int CantidadInteracciones { get; set; }
}

/// <summary>
/// Ficha completa del comercio, con sus interacciones.
/// </summary>
public class ComercioDetalleDto
{
    public int Id { get; set; }
    public string NombreComercial { get; set; } = string.Empty;
    public string Cuit { get; set; } = string.Empty;
    public string NombreContacto { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Email { get; set; }

    public int RubroId { get; set; }
    public string Rubro { get; set; } = string.Empty;

    public EstadoComercioEnum Estado { get; set; }
    public string EstadoNombre { get; set; } = string.Empty;

    /// <summary>Estados a los que este comercio puede pasar desde donde esta.</summary>
    public IReadOnlyList<EstadoComercioEnum> TransicionesPosibles { get; set; } = [];

    public string? Notas { get; set; }
    public DateTime FechaCreacion { get; set; }
    public DateTime? FechaActualizacion { get; set; }

    /// <summary>
    /// Token de concurrencia. Viaja tambien en el header ETag; se incluye en el
    /// cuerpo para que un cliente que no maneje headers pueda usarlo igual.
    /// </summary>
    public uint Version { get; set; }

    public IReadOnlyList<InteraccionDto> Interacciones { get; set; } = [];
}

public class CrearComercioDto
{
    public string NombreComercial { get; set; } = string.Empty;
    public string Cuit { get; set; } = string.Empty;
    public string NombreContacto { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public int RubroId { get; set; }
    public string? Notas { get; set; }
}

/// <summary>
/// Datos editables del comercio. El estado no esta aca a proposito: se cambia
/// por su propio endpoint, porque tiene reglas de transicion que un PUT
/// generico no deberia poder saltear.
/// </summary>
public class ActualizarComercioDto
{
    public string NombreComercial { get; set; } = string.Empty;
    public string Cuit { get; set; } = string.Empty;
    public string NombreContacto { get; set; } = string.Empty;
    public string? Telefono { get; set; }
    public string? Email { get; set; }
    public int RubroId { get; set; }
    public string? Notas { get; set; }
}

public class CambiarEstadoDto
{
    public EstadoComercioEnum NuevoEstado { get; set; }
}

/// <summary>
/// Parametros de busqueda, filtro, orden y paginacion del listado.
/// Llega por query string.
/// </summary>
public class ComercioFiltroDto
{
    private const int TamanoMaximo = 100;

    private int _pagina = 1;
    private int _tamanoPagina = 20;

    /// <summary>Texto libre. Se resuelve con full text search sobre search_vector.</summary>
    public string? Busqueda { get; set; }

    public EstadoComercioEnum? Estado { get; set; }

    public int? RubroId { get; set; }

    /// <summary>Campo por el que ordenar. Ver <c>OrdenComercio</c>.</summary>
    public string? OrdenarPor { get; set; }

    public bool Descendente { get; set; } = true;

    public int Pagina
    {
        get => _pagina;
        // Se corrige en lugar de rechazar: una pagina 0 o negativa es un error
        // del cliente que no justifica devolverle un 400.
        set => _pagina = value < 1 ? 1 : value;
    }

    public int TamanoPagina
    {
        get => _tamanoPagina;
        // El tope evita que alguien pida un millon de filas de una y tumbe la base.
        set => _tamanoPagina = value switch
        {
            < 1 => 20,
            > TamanoMaximo => TamanoMaximo,
            _ => value
        };
    }
}
