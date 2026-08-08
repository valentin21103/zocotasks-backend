namespace ZocoTasks.Business.DTOs;

/// <summary>
/// Resultado paginado. Devuelve los datos de la pagina mas lo que el front
/// necesita para dibujar el paginador sin tener que calcular nada.
/// </summary>
public class PagedResult<T>
{
    public IReadOnlyList<T> Items { get; set; } = [];

    /// <summary>Total de registros que matchean el filtro, no los de esta pagina.</summary>
    public int Total { get; set; }

    public int Pagina { get; set; }

    public int TamanoPagina { get; set; }

    public int TotalPaginas =>
        TamanoPagina <= 0 ? 0 : (int)Math.Ceiling(Total / (double)TamanoPagina);

    public bool HayAnterior => Pagina > 1;

    public bool HaySiguiente => Pagina < TotalPaginas;
}
