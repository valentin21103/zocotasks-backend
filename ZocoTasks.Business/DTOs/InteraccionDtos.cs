using ZocoTasks.Domain.Enums;

namespace ZocoTasks.Business.DTOs;

public class InteraccionDto
{
    public int Id { get; set; }
    public int ComercioId { get; set; }

    public TipoInteraccionEnum Tipo { get; set; }
    public string TipoNombre { get; set; } = string.Empty;

    /// <summary>Cuando ocurrio el contacto, que puede ser anterior a la carga.</summary>
    public DateTime Fecha { get; set; }

    public string Detalle { get; set; } = string.Empty;

    public DateTime FechaCreacion { get; set; }
}

public class CrearInteraccionDto
{
    public TipoInteraccionEnum Tipo { get; set; }

    /// <summary>Si no se envia, se toma el momento actual.</summary>
    public DateTime? Fecha { get; set; }

    public string Detalle { get; set; } = string.Empty;
}
