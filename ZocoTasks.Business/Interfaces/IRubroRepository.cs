using ZocoTasks.Business.DTOs;
using ZocoTasks.Domain.Entities;

namespace ZocoTasks.Business.Interfaces;

public interface IRubroRepository
{
    Task<IReadOnlyList<RubroAbmDto>> Listar(CancellationToken ct);

    Task<Rubro?> ObtenerPorId(int id, CancellationToken ct);

    /// <summary>
    /// Comparacion sin distinguir mayusculas: el indice unico de la base es
    /// sensible a mayusculas, asi que sin esto "Kiosco" y "kiosco" entrarian
    /// como dos rubros distintos.
    /// </summary>
    Task<bool> ExisteNombre(string nombre, int? exceptoId, CancellationToken ct);

    Task<int> ContarComercios(int rubroId, CancellationToken ct);

    void Agregar(Rubro rubro);

    void Eliminar(Rubro rubro);

    Task Guardar(CancellationToken ct);
}
