using ZocoTasks.Business.DTOs;

namespace ZocoTasks.Business.Interfaces;

/// <summary>
/// Puerto hacia el proveedor de IA.
/// </summary>
public interface IProveedorAnalisisIA
{
    /// <summary>Nombre del modelo, para informarlo en la respuesta.</summary>
    string Modelo { get; }

    /// <summary>
    /// Manda el contexto al modelo y devuelve el analisis ya estructurado.
    /// </summary>
    /// <exception cref="Exception">
    /// Lanza ante cualquier fallo del proveedor. Que hacer con ese fallo lo
    /// decide <c>AnalisisService</c>, porque degradar o no es una politica de
    /// negocio y no un detalle del transporte.
    /// </exception>
      Task<AnalisisOportunidadDto> Analizar(
        string instruccionSistema,
        string contexto,
        CancellationToken ct);
}
