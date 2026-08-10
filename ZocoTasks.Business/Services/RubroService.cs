using FluentValidation;
using ZocoTasks.Business.DTOs;
using ZocoTasks.Business.Interfaces;
using ZocoTasks.Domain.Entities;
using ZocoTasks.Domain.Exceptions;

namespace ZocoTasks.Business.Services;

public class RubroService : IRubroService
{
    private readonly IRubroRepository _repository;
    private readonly IValidator<GuardarRubroDto> _validador;

    public RubroService(IRubroRepository repository, IValidator<GuardarRubroDto> validador)
    {
        _repository = repository;
        _validador = validador;
    }

    public async Task<IReadOnlyList<RubroAbmDto>> Listar(CancellationToken ct)
    {
        return await _repository.Listar(ct);
    }

    public async Task<RubroAbmDto> Crear(GuardarRubroDto dto, CancellationToken ct)
    {
        await _validador.ValidateAndThrowAsync(dto, ct);

        var nombre = dto.Nombre.Trim();
        await ValidarNombreDisponible(nombre, null, ct);

        var rubro = new Rubro { Nombre = nombre, Activo = true };

        _repository.Agregar(rubro);
        await _repository.Guardar(ct);

        return new RubroAbmDto
        {
            Id = rubro.Id,
            Nombre = rubro.Nombre,
            Activo = rubro.Activo,
            CantidadComercios = 0
        };
    }

    public async Task<RubroAbmDto> Actualizar(int id, GuardarRubroDto dto, CancellationToken ct)
    {
        await _validador.ValidateAndThrowAsync(dto, ct);

        var rubro = await _repository.ObtenerPorId(id, ct)
            ?? throw new EntidadNoEncontradaException("Rubro", id);

        var nombre = dto.Nombre.Trim();
        await ValidarNombreDisponible(nombre, id, ct);

        rubro.Nombre = nombre;
        rubro.Activo = dto.Activo;

        await _repository.Guardar(ct);

        return new RubroAbmDto
        {
            Id = rubro.Id,
            Nombre = rubro.Nombre,
            Activo = rubro.Activo,
            CantidadComercios = await _repository.ContarComercios(id, ct)
        };
    }

    /// <summary>
    /// La baja tiene dos comportamientos, y cual se aplica depende de si el
    /// rubro esta en uso.
    /// </summary>
    /// <remarks>
    /// Sin comercios asociados se borra de verdad: el caso tipico es el rubro
    /// creado por error, y dejarlo desactivado para siempre seria ensuciar la
    /// tabla con basura.
    ///
    /// Con comercios asociados **no se puede borrar**, porque la FK se llevaria
    /// puestos los comercios o fallaria. Ahi se desactiva: desaparece del combo
    /// para los comercios nuevos y los historicos siguen mostrandolo. Para eso
    /// existe la columna <c>activo</c>.
    /// </remarks>
    public async Task<ResultadoBajaRubroDto> Eliminar(int id, CancellationToken ct)
    {
        var rubro = await _repository.ObtenerPorId(id, ct)
            ?? throw new EntidadNoEncontradaException("Rubro", id);

        var comercios = await _repository.ContarComercios(id, ct);

        if (comercios == 0)
        {
            _repository.Eliminar(rubro);
        }
        else
        {
            rubro.Activo = false;
        }

        await _repository.Guardar(ct);

        return new ResultadoBajaRubroDto
        {
            Eliminado = comercios == 0,
            ComerciosAsociados = comercios
        };
    }

    private async Task ValidarNombreDisponible(string nombre, int? exceptoId, CancellationToken ct)
    {
        if (await _repository.ExisteNombre(nombre, exceptoId, ct))
        {
            throw new ReglaDeNegocioException($"Ya existe un rubro con el nombre '{nombre}'.");
        }
    }
}
