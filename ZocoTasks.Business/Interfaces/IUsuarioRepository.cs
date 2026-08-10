using ZocoTasks.Domain.Entities;

namespace ZocoTasks.Business.Interfaces;

public interface IUsuarioRepository
{
    /// <summary>Trae el usuario con sus roles. Null si no existe.</summary>
    Task<Usuario?> BuscarPorEmail(string email, CancellationToken ct);
}
