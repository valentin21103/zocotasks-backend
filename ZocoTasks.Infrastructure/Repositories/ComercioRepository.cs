using Microsoft.EntityFrameworkCore;
using NpgsqlTypes;
using ZocoTasks.Business.DTOs;
using ZocoTasks.Business.Interfaces;
using ZocoTasks.Domain.Entities;
using ZocoTasks.Infrastructure.Data;
using ZocoTasks.Infrastructure.Data.Configurations;

namespace ZocoTasks.Infrastructure.Repositories;

public class ComercioRepository : IComercioRepository
{
    private readonly ZocoDbContext _context;

    public ComercioRepository(ZocoDbContext context)
    {
        _context = context;
    }

    public async Task<PagedResult<ComercioListItemDto>> Listar(
        ComercioFiltroDto filtro, CancellationToken ct)
    {
        // AsNoTracking: el listado es de solo lectura, no hace falta que EF
        // arme el change tracker para cada fila.
        var query = _context.Comercios.AsNoTracking();

        // --- Busqueda full text ------------------------------------------
        // Se compara contra la columna generada search_vector usando el indice
        // GIN. PlainToTsQuery aplica al termino buscado el mismo procesamiento
        // (stemming, stopwords) que se aplico al indexar, que es lo que hace
        // que "cobrar" encuentre "cobra".
        if (!string.IsNullOrWhiteSpace(filtro.Busqueda))
        {
            var termino = filtro.Busqueda.Trim();
            query = query.Where(c =>
                EF.Property<NpgsqlTsVector>(c, ComercioConfiguration.SearchVectorProperty)
                    .Matches(EF.Functions.PlainToTsQuery("spanish", termino)));
        }

        // --- Filtros ------------------------------------------------------
        if (filtro.Estado.HasValue)
        {
            query = query.Where(c => c.Estado == filtro.Estado.Value);
        }

        if (filtro.RubroId.HasValue)
        {
            query = query.Where(c => c.RubroId == filtro.RubroId.Value);
        }

        // El total se cuenta con los filtros aplicados pero antes de paginar:
        // es la cantidad de resultados, no la de la pagina.
        var total = await query.CountAsync(ct);

        query = AplicarOrden(query, filtro.OrdenarPor, filtro.Descendente);

        var items = await query
            .Skip((filtro.Pagina - 1) * filtro.TamanoPagina)
            .Take(filtro.TamanoPagina)
            // La proyeccion se hace en SQL: solo viajan las columnas que el
            // listado muestra, y el conteo de interacciones se resuelve con un
            // subquery en vez de traerlas todas.
            .Select(c => new ComercioListItemDto
            {
                Id = c.Id,
                NombreComercial = c.NombreComercial,
                Cuit = c.Cuit,
                NombreContacto = c.NombreContacto,
                Telefono = c.Telefono,
                Email = c.Email,
                RubroId = c.RubroId,
                Rubro = c.Rubro.Nombre,
                Estado = c.Estado,
                EstadoNombre = c.EstadoNavegacion.Nombre,
                FechaCreacion = c.FechaCreacion,
                CantidadInteracciones = c.Interacciones.Count
            })
            .ToListAsync(ct);

        return new PagedResult<ComercioListItemDto>
        {
            Items = items,
            Total = total,
            Pagina = filtro.Pagina,
            TamanoPagina = filtro.TamanoPagina
        };
    }

    /// <summary>
    /// Orden por lista blanca. No se concatena el nombre de campo en SQL: eso
    /// seria una puerta de inyeccion. Lo que no esta en el switch cae al
    /// default.
    /// </summary>
    private static IQueryable<Comercio> AplicarOrden(
        IQueryable<Comercio> query, string? campo, bool descendente)
    {
        return (campo?.ToLowerInvariant()) switch
        {
            "nombre" or "nombrecomercial" => descendente
                ? query.OrderByDescending(c => c.NombreComercial)
                : query.OrderBy(c => c.NombreComercial),

            "estado" => descendente
                ? query.OrderByDescending(c => c.Estado).ThenByDescending(c => c.FechaCreacion)
                : query.OrderBy(c => c.Estado).ThenByDescending(c => c.FechaCreacion),

            "rubro" => descendente
                ? query.OrderByDescending(c => c.Rubro.Nombre)
                : query.OrderBy(c => c.Rubro.Nombre),

            "contacto" or "nombrecontacto" => descendente
                ? query.OrderByDescending(c => c.NombreContacto)
                : query.OrderBy(c => c.NombreContacto),

            // Por defecto, los mas nuevos primero: es lo que un vendedor espera
            // al abrir el listado.
            _ => descendente
                ? query.OrderByDescending(c => c.FechaCreacion).ThenByDescending(c => c.Id)
                : query.OrderBy(c => c.FechaCreacion).ThenBy(c => c.Id)
        };
    }

    public async Task<Comercio?> ObtenerConDetalle(int id, CancellationToken ct)
    {
        return await _context.Comercios
            .Include(c => c.Rubro)
            .Include(c => c.EstadoNavegacion)
            .Include(c => c.Interacciones.OrderByDescending(i => i.Fecha))
                .ThenInclude(i => i.TipoNavegacion)
            .FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<Comercio?> ObtenerParaEditar(int id, CancellationToken ct)
    {
        return await _context.Comercios.FirstOrDefaultAsync(c => c.Id == id, ct);
    }

    public async Task<bool> ExisteCuit(string cuit, int? exceptoId, CancellationToken ct)
    {
        // IgnoreQueryFilters: el indice unico de CUIT tambien alcanza a los
        // comercios dados de baja, asi que hay que mirarlos para poder dar un
        // mensaje claro en vez de dejar que explote la constraint.
        var query = _context.Comercios.IgnoreQueryFilters().Where(c => c.Cuit == cuit);

        if (exceptoId.HasValue)
        {
            query = query.Where(c => c.Id != exceptoId.Value);
        }

        return await query.AnyAsync(ct);
    }

    public async Task<bool> ExisteRubro(int rubroId, CancellationToken ct)
    {
        return await _context.Rubros.AnyAsync(r => r.Id == rubroId && r.Activo, ct);
    }

    public void Agregar(Comercio comercio)
    {
        _context.Comercios.Add(comercio);
    }

    public async Task GuardarConControlDeConcurrencia(
        Comercio comercio, uint versionEsperada, CancellationToken ct)
    {
        // Aca esta el nucleo de la concurrencia optimista. Se le dice a EF que
        // el valor original de xmin es el que el cliente mando en If-Match, no
        // el que se leyo de la base. Asi el UPDATE queda con
        // "WHERE id = @id AND xmin = @versionEsperada": si otro usuario grabo
        // en el medio, afecta cero filas y EF lanza
        // DbUpdateConcurrencyException, que el middleware traduce a 409.
        _context.Entry(comercio).Property(c => c.Version).OriginalValue = versionEsperada;

        await _context.SaveChangesAsync(ct);
    }

    public async Task Guardar(CancellationToken ct)
    {
        await _context.SaveChangesAsync(ct);
    }
}
