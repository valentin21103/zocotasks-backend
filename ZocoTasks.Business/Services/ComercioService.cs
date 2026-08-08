using ZocoTasks.Business.DTOs;
using ZocoTasks.Business.Interfaces;
using ZocoTasks.Domain.Common;
using ZocoTasks.Domain.Entities;
using ZocoTasks.Domain.Enums;
using ZocoTasks.Domain.Exceptions;

namespace ZocoTasks.Business.Services;

public class ComercioService : IComercioService
{
    private readonly IComercioRepository _repository;

    public ComercioService(IComercioRepository repository)
    {
        _repository = repository;
    }

    public async Task<PagedResult<ComercioListItemDto>> Listar(
        ComercioFiltroDto filtro, CancellationToken ct)
    {
        return await _repository.Listar(filtro, ct);
    }

    public async Task<ComercioDetalleDto> ObtenerPorId(int id, CancellationToken ct)
    {
        var comercio = await _repository.ObtenerConDetalle(id, ct);

        if (comercio == null)
        {
            throw new EntidadNoEncontradaException("Comercio", id);
        }

        return Mapear(comercio);
    }

    public async Task<ComercioDetalleDto> Crear(CrearComercioDto dto, CancellationToken ct)
    {
        // El formato del CUIT ya lo valido FluentValidation; aca se controla lo
        // que solo puede saberse consultando la base.
        var cuit = Cuit.Normalizar(dto.Cuit);

        await ValidarCuitDisponible(cuit, null, ct);
        await ValidarRubro(dto.RubroId, ct);

        var comercio = new Comercio
        {
            NombreComercial = dto.NombreComercial.Trim(),
            Cuit = cuit,
            NombreContacto = dto.NombreContacto.Trim(),
            Telefono = Limpiar(dto.Telefono),
            Email = Limpiar(dto.Email),
            RubroId = dto.RubroId,
            Notas = Limpiar(dto.Notas),
            Estado = EstadoComercioEnum.Nuevo
        };

        _repository.Agregar(comercio);
        await _repository.Guardar(ct);

        // Se relee para devolver el nombre del rubro y del estado, que son de
        // las tablas relacionadas y no estan cargados en la entidad recien
        // insertada.
        return await ObtenerPorId(comercio.Id, ct);
    }

    public async Task<ComercioDetalleDto> Actualizar(
        int id, ActualizarComercioDto dto, uint versionEsperada, CancellationToken ct)
    {
        var comercio = await _repository.ObtenerParaEditar(id, ct);

        if (comercio == null)
        {
            throw new EntidadNoEncontradaException("Comercio", id);
        }

        var cuit = Cuit.Normalizar(dto.Cuit);

        await ValidarCuitDisponible(cuit, id, ct);
        await ValidarRubro(dto.RubroId, ct);

        comercio.NombreComercial = dto.NombreComercial.Trim();
        comercio.Cuit = cuit;
        comercio.NombreContacto = dto.NombreContacto.Trim();
        comercio.Telefono = Limpiar(dto.Telefono);
        comercio.Email = Limpiar(dto.Email);
        comercio.RubroId = dto.RubroId;
        comercio.Notas = Limpiar(dto.Notas);

        await _repository.GuardarConControlDeConcurrencia(comercio, versionEsperada, ct);

        return await ObtenerPorId(id, ct);
    }

    public async Task<ComercioDetalleDto> CambiarEstado(
        int id, EstadoComercioEnum nuevoEstado, uint versionEsperada, CancellationToken ct)
    {
        var comercio = await _repository.ObtenerParaEditar(id, ct);

        if (comercio == null)
        {
            throw new EntidadNoEncontradaException("Comercio", id);
        }

        // La validacion de la transicion la hace el dominio, no este servicio:
        // si lanza, el middleware lo traduce a 409.
        comercio.CambiarEstado(nuevoEstado);

        await _repository.GuardarConControlDeConcurrencia(comercio, versionEsperada, ct);

        return await ObtenerPorId(id, ct);
    }

    public async Task Eliminar(int id, CancellationToken ct)
    {
        var comercio = await _repository.ObtenerParaEditar(id, ct);

        if (comercio == null)
        {
            throw new EntidadNoEncontradaException("Comercio", id);
        }

        // Baja logica: marcar la fecha alcanza, porque el filtro global de EF
        // lo saca de todas las consultas. Un borrado fisico se llevaria por
        // cascada las interacciones, que son la evidencia del seguimiento.
        comercio.FechaEliminacion = DateTime.UtcNow;

        await _repository.Guardar(ct);
    }

    // -----------------------------------------------------------------
    // Auxiliares
    // -----------------------------------------------------------------

    private async Task ValidarCuitDisponible(string cuit, int? exceptoId, CancellationToken ct)
    {
        if (await _repository.ExisteCuit(cuit, exceptoId, ct))
        {
            throw new ReglaDeNegocioException(
                $"Ya existe un comercio registrado con el CUIT {cuit}.");
        }
    }

    private async Task ValidarRubro(int rubroId, CancellationToken ct)
    {
        if (!await _repository.ExisteRubro(rubroId, ct))
        {
            throw new ReglaDeNegocioException(
                $"El rubro {rubroId} no existe o esta dado de baja.");
        }
    }

    /// <summary>Convierte cadenas vacias o de solo espacios en null.</summary>
    private static string? Limpiar(string? valor) =>
        string.IsNullOrWhiteSpace(valor) ? null : valor.Trim();

    private static ComercioDetalleDto Mapear(Comercio c)
    {
        return new ComercioDetalleDto
        {
            Id = c.Id,
            NombreComercial = c.NombreComercial,
            Cuit = c.Cuit,
            NombreContacto = c.NombreContacto,
            Telefono = c.Telefono,
            Email = c.Email,
            RubroId = c.RubroId,
            Rubro = c.Rubro?.Nombre ?? string.Empty,
            Estado = c.Estado,
            EstadoNombre = c.EstadoNavegacion?.Nombre ?? c.Estado.ToString(),
            TransicionesPosibles = [.. MaquinaEstadoComercio.TransicionesDesde(c.Estado)],
            Notas = c.Notas,
            FechaCreacion = c.FechaCreacion,
            FechaActualizacion = c.FechaActualizacion,
            Version = c.Version,
            Interacciones = [.. c.Interacciones.Select(i => new InteraccionDto
            {
                Id = i.Id,
                ComercioId = i.ComercioId,
                Tipo = i.Tipo,
                TipoNombre = i.TipoNavegacion?.Nombre ?? i.Tipo.ToString(),
                Fecha = i.Fecha,
                Detalle = i.Detalle,
                FechaCreacion = i.FechaCreacion
            })]
        };
    }
}
