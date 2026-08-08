using ZocoTasks.Domain.Entities;
using ZocoTasks.Domain.Enums;
using ZocoTasks.Domain.Exceptions;

namespace ZocoTasks.Tests.Unit;

/// <summary>
/// <c>Comercio.CambiarEstado</c> es el unico camino para mover un comercio en el
/// pipeline. Estos tests verifican que efectivamente no se pueda esquivar la
/// validacion: si alguien agregara otra via para escribir el estado, el segundo
/// test de esta clase deja de tener sentido y hay que revisar el diseno.
/// </summary>
public class ComercioCambiarEstadoTests
{
    private static Comercio NuevoComercio(EstadoComercioEnum estado = EstadoComercioEnum.Nuevo) =>
        new()
        {
            NombreComercial = "Parrilla Don Zoco",
            Cuit = "20123456789",
            NombreContacto = "Juan Perez",
            RubroId = 1,
            Estado = estado
        };

    [Fact]
    public void CambiarEstadoAvanzaCuandoLaTransicionEsValida()
    {
        var comercio = NuevoComercio();

        comercio.CambiarEstado(EstadoComercioEnum.Contactado);

        Assert.Equal(EstadoComercioEnum.Contactado, comercio.Estado);
    }

    [Fact]
    public void CambiarEstadoLanzaYDejaElEstadoIntactoSiLaTransicionEsInvalida()
    {
        var comercio = NuevoComercio();

        Assert.Throws<EstadoTransicionInvalidaException>(() =>
            comercio.CambiarEstado(EstadoComercioEnum.Aprobado));

        // Lo importante no es solo que lance, sino que no haya mutado a medias.
        Assert.Equal(EstadoComercioEnum.Nuevo, comercio.Estado);
    }

    [Fact]
    public void UnComercioPuedeRecorrerElEmbudoCompletoHastaAprobado()
    {
        var comercio = NuevoComercio();

        comercio.CambiarEstado(EstadoComercioEnum.Contactado);
        comercio.CambiarEstado(EstadoComercioEnum.Interesado);
        comercio.CambiarEstado(EstadoComercioEnum.Documentacion);
        comercio.CambiarEstado(EstadoComercioEnum.Aprobado);

        Assert.Equal(EstadoComercioEnum.Aprobado, comercio.Estado);
    }

    [Fact]
    public void UnComercioAprobadoYaNoSePuedeMover()
    {
        var comercio = NuevoComercio(EstadoComercioEnum.Aprobado);

        Assert.Throws<EstadoTransicionInvalidaException>(() =>
            comercio.CambiarEstado(EstadoComercioEnum.Rechazado));
    }

    [Fact]
    public void UnComercioNuevoArrancaSinInteracciones()
    {
        Assert.Empty(NuevoComercio().Interacciones);
    }
}
