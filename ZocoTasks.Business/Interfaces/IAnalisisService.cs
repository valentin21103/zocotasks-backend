using ZocoTasks.Business.DTOs;

namespace ZocoTasks.Business.Interfaces;

public interface IAnalisisService
{
    /// <summary>
    /// Analiza la oportunidad con la informacion actual del comercio.
    /// </summary>
    Task<AnalisisOportunidadDto> Analizar(int comercioId, CancellationToken ct);
}
