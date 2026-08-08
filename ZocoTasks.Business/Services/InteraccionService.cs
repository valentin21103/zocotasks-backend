using FluentValidation;
using ZocoTasks.Business.DTOs;
using ZocoTasks.Business.Interfaces;
using ZocoTasks.Domain.Entities;
using ZocoTasks.Domain.Exceptions;

namespace ZocoTasks.Business.Services;

public class InteraccionService : IInteraccionService
{
    private readonly IInteraccionRepository _repository;
    private readonly IValidator<CrearInteraccionDto> _validadorCrear;

    public InteraccionService(
        IInteraccionRepository repository,
        IValidator<CrearInteraccionDto> validadorCrear)
    {
        _repository = repository;
        _validadorCrear = validadorCrear;
    }

    public async Task<IReadOnlyList<InteraccionDto>> ListarPorComercio(
        int comercioId, CancellationToken ct)
    {
        await ValidarComercio(comercioId, ct);

        return await _repository.ListarPorComercio(comercioId, ct);
    }

    public async Task<InteraccionDto> Crear(
        int comercioId, CrearInteraccionDto dto, CancellationToken ct)
    {
        await _validadorCrear.ValidateAndThrowAsync(dto, ct);
        await ValidarComercio(comercioId, ct);

        var interaccion = new Interaccion
        {
            ComercioId = comercioId,
            Tipo = dto.Tipo,
            // Si no mandan fecha, se asume que la interaccion es de ahora. Se
            // permite mandarla porque muchas se cargan despues de que ocurren.
            Fecha = dto.Fecha ?? DateTime.UtcNow,
            Detalle = dto.Detalle.Trim()
        };

        _repository.Agregar(interaccion);
        await _repository.Guardar(ct);

        // Se relee la lista para devolver el item con el nombre del tipo, que
        // viene de la tabla de catalogo.
        var creada = (await _repository.ListarPorComercio(comercioId, ct))
            .First(i => i.Id == interaccion.Id);

        return creada;
    }

    public async Task Eliminar(int comercioId, int interaccionId, CancellationToken ct)
    {
        var interaccion = await _repository.ObtenerPorId(comercioId, interaccionId, ct);

        if (interaccion == null)
        {
            throw new EntidadNoEncontradaException("Interaccion", interaccionId);
        }

        // Borrado fisico, a diferencia del comercio: una interaccion cargada
        // por error no tiene valor historico y no cuelga nada de ella.
        _repository.Eliminar(interaccion);
        await _repository.Guardar(ct);
    }

    private async Task ValidarComercio(int comercioId, CancellationToken ct)
    {
        if (!await _repository.ExisteComercio(comercioId, ct))
        {
            throw new EntidadNoEncontradaException("Comercio", comercioId);
        }
    }
}
