namespace ZocoTasks.Business.DTOs;

/// <summary>
/// Item generico de catalogo. Lo consume el front para llenar los combos de
/// estado, rubro y tipo de interaccion sin hardcodear las listas.
/// </summary>
public class CatalogoItemDto
{
    public int Id { get; set; }
    public string Nombre { get; set; } = string.Empty;
}

public class EstadoCatalogoDto : CatalogoItemDto
{
    public string Codigo { get; set; } = string.Empty;

    /// <summary>Posicion en el embudo, para ordenar el pipeline en pantalla.</summary>
    public short Orden { get; set; }

    public bool EsFinal { get; set; }
}
