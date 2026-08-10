using Microsoft.Extensions.Logging.Abstractions;
using ZocoTasks.Business.DTOs;
using ZocoTasks.Business.Interfaces;
using ZocoTasks.Business.Services;
using ZocoTasks.Domain.Entities;
using ZocoTasks.Domain.Enums;
using ZocoTasks.Domain.Exceptions;

namespace ZocoTasks.Tests.Unit;

/// <summary>
/// "Analizar oportunidad".
/// </summary>
/// <remarks>
/// Estos tests no tocan la red ni Gemini: el puerto <c>IProveedorAnalisisIA</c>
/// se reemplaza por una implementacion de prueba. Eso permite verificar lo que
/// realmente importa del servicio —que un fallo del proveedor no rompa la
/// aplicacion— sin depender de que haya API key ni conexion.
/// </remarks>
public class AnalisisServiceTests
{
    // -----------------------------------------------------------------
    // Dobles de prueba
    // -----------------------------------------------------------------

    private sealed class ProveedorQueResponde : IProveedorAnalisisIA
    {
        public string Modelo => "modelo-de-prueba";

        public string? ContextoRecibido { get; private set; }

        public Task<AnalisisOportunidadDto> Analizar(
            string instruccionSistema, string contexto, CancellationToken ct)
        {
            ContextoRecibido = contexto;

            return Task.FromResult(new AnalisisOportunidadDto
            {
                Resumen = "Comercio gastronomico con interes concreto.",
                NivelInteres = NivelInteres.Alto,
                ProximoPaso = "Coordinar demo de POS + QR.",
                PreguntasSugeridas = ["¿Volumen mensual?", "¿Cuantas cajas?", "¿Cuantas sucursales?"],
                DatosFaltantes = ["Volumen mensual aproximado"]
            });
        }
    }

    private sealed class ProveedorQueFalla : IProveedorAnalisisIA
    {
        public string Modelo => "modelo-de-prueba";

        public Task<AnalisisOportunidadDto> Analizar(
            string instruccionSistema, string contexto, CancellationToken ct) =>
            throw new HttpRequestException("Gemini respondio 429: cuota agotada");
    }

    private sealed class RepositorioFalso : IComercioRepository
    {
        private readonly Comercio? _comercio;

        public RepositorioFalso(Comercio? comercio) => _comercio = comercio;

        public Task<Comercio?> ObtenerConDetalle(int id, CancellationToken ct) =>
            Task.FromResult(_comercio);

        // El resto del contrato no participa de este caso de uso.
        public Task<PagedResult<ComercioListItemDto>> Listar(ComercioFiltroDto f, CancellationToken ct) =>
            throw new NotSupportedException();
        public Task<Comercio?> ObtenerParaEditar(int id, CancellationToken ct) =>
            throw new NotSupportedException();
        public Task<bool> ExisteCuit(string cuit, int? exceptoId, CancellationToken ct) =>
            throw new NotSupportedException();
        public Task<bool> ExisteRubro(int rubroId, CancellationToken ct) =>
            throw new NotSupportedException();
        public void Agregar(Comercio comercio) => throw new NotSupportedException();
        public Task GuardarConControlDeConcurrencia(Comercio c, uint v, CancellationToken ct) =>
            throw new NotSupportedException();
        public Task Guardar(CancellationToken ct) => throw new NotSupportedException();
    }

    private static Comercio ComercioDePrueba()
    {
        var comercio = new Comercio
        {
            Id = 1,
            NombreComercial = "Parrilla Don Zoco",
            Cuit = "20123456786",
            NombreContacto = "Juan Perez",
            Telefono = "+54 351 555-1234",
            Email = "juan@mail.com",
            RubroId = 1,
            Rubro = new Rubro { Id = 1, Nombre = "Gastronomia" },
            Estado = EstadoComercioEnum.Contactado,
            Notas = "Dos sucursales. Problemas de conciliacion.",
            FechaCreacion = DateTime.UtcNow.AddDays(-12)
        };

        comercio.Interacciones.Add(new Interaccion
        {
            Id = 1,
            ComercioId = 1,
            Tipo = TipoInteraccionEnum.Llamada,
            Fecha = DateTime.UtcNow.AddDays(-3),
            Detalle = "Consulto por conciliacion automatica."
        });

        return comercio;
    }

    private static AnalisisService Servicio(
        IComercioRepository repositorio, IProveedorAnalisisIA proveedor) =>
        new(repositorio, proveedor, NullLogger<AnalisisService>.Instance);

    // -----------------------------------------------------------------
    // Camino feliz
    // -----------------------------------------------------------------

    [Fact]
    public async Task DevuelveElAnalisisDelProveedorYLoCompletaConElModeloYLaFecha()
    {
        var proveedor = new ProveedorQueResponde();
        var servicio = Servicio(new RepositorioFalso(ComercioDePrueba()), proveedor);

        var analisis = await servicio.Analizar(1, CancellationToken.None);

        Assert.False(analisis.EsDegradado);
        Assert.Equal(NivelInteres.Alto, analisis.NivelInteres);
        Assert.Equal(3, analisis.PreguntasSugeridas.Count);
        Assert.Equal("modelo-de-prueba", analisis.ModeloUtilizado);
        Assert.NotEqual(default, analisis.FechaGeneracion);
    }

    // -----------------------------------------------------------------
    // Lo que se le manda al modelo
    // -----------------------------------------------------------------

    [Fact]
    public async Task ElContextoIncluyeLasNotasYLasInteracciones()
    {
        var proveedor = new ProveedorQueResponde();
        var servicio = Servicio(new RepositorioFalso(ComercioDePrueba()), proveedor);

        await servicio.Analizar(1, CancellationToken.None);

        var contexto = proveedor.ContextoRecibido!;

        Assert.Contains("Parrilla Don Zoco", contexto);
        Assert.Contains("Gastronomia", contexto);
        Assert.Contains("Contactado", contexto);
        Assert.Contains("Problemas de conciliacion", contexto);
        Assert.Contains("Consulto por conciliacion automatica", contexto);
    }

    [Fact]
    public async Task ElContextoNoFiltraDatosPersonalesAlProveedorExterno()
    {
        var proveedor = new ProveedorQueResponde();
        var servicio = Servicio(new RepositorioFalso(ComercioDePrueba()), proveedor);

        await servicio.Analizar(1, CancellationToken.None);

        var contexto = proveedor.ContextoRecibido!;

        // CUIT, telefono y email no aportan nada a la evaluacion comercial:
        // mandarlos a un tercero seria exponerlos sin necesidad.
        Assert.DoesNotContain("20123456786", contexto);
        Assert.DoesNotContain("555-1234", contexto);
        Assert.DoesNotContain("juan@mail.com", contexto);

        // De esos campos solo viaja si estan cargados, que es lo que el modelo
        // necesita para detectar datos faltantes.
        Assert.Contains("Telefono cargado: si", contexto);
    }

    [Fact]
    public async Task UnComercioSinInteraccionesLoDiceExplicitamente()
    {
        var comercio = ComercioDePrueba();
        comercio.Interacciones.Clear();
        comercio.Notas = null;

        var proveedor = new ProveedorQueResponde();
        var servicio = Servicio(new RepositorioFalso(comercio), proveedor);

        await servicio.Analizar(1, CancellationToken.None);

        var contexto = proveedor.ContextoRecibido!;

        // Que no haya datos es informacion, no un hueco: el modelo tiene que
        // verlo para poder responder Indeterminado en vez de inventar.
        Assert.Contains("sin notas cargadas", contexto);
        Assert.Contains("todavia no se registro ninguna interaccion", contexto);
    }

    // -----------------------------------------------------------------
    // Degradacion: el requisito de que un fallo externo no rompa la app
    // -----------------------------------------------------------------

    [Fact]
    public async Task SiElProveedorFallaDevuelveRespuestaDegradadaYNoPropaga()
    {
        var servicio = Servicio(new RepositorioFalso(ComercioDePrueba()), new ProveedorQueFalla());

        var analisis = await servicio.Analizar(1, CancellationToken.None);

        Assert.True(analisis.EsDegradado);
        Assert.NotEmpty(analisis.Resumen);
    }

    [Fact]
    public async Task LaRespuestaDegradadaNoInventaUnNivelDeInteres()
    {
        var servicio = Servicio(new RepositorioFalso(ComercioDePrueba()), new ProveedorQueFalla());

        var analisis = await servicio.Analizar(1, CancellationToken.None);

        // Si el modelo no respondio, el sistema no adivina. Es la misma regla
        // que el prompt le exige al modelo, aplicada al sistema.
        Assert.Equal(NivelInteres.Indeterminado, analisis.NivelInteres);
        Assert.Empty(analisis.PreguntasSugeridas);
        Assert.Empty(analisis.DatosFaltantes);
    }

    // -----------------------------------------------------------------
    // Comercio inexistente
    // -----------------------------------------------------------------

    [Fact]
    public async Task UnComercioInexistenteLanzaNoEncontradoYNoLlamaAlProveedor()
    {
        var proveedor = new ProveedorQueResponde();
        var servicio = Servicio(new RepositorioFalso(null), proveedor);

        await Assert.ThrowsAsync<EntidadNoEncontradaException>(
            () => servicio.Analizar(999, CancellationToken.None));

        // No gastar cuota del proveedor por un id que no existe.
        Assert.Null(proveedor.ContextoRecibido);
    }
}
