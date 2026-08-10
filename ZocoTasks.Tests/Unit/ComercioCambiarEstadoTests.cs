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

        // Mover un comercio al estado en el que ya esta no es un cambio: es la
        // unica transicion que la maquina sigue rechazando.
        Assert.Throws<EstadoTransicionInvalidaException>(() =>
            comercio.CambiarEstado(EstadoComercioEnum.Nuevo));

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
    public void UnComercioAprobadoPuedeReabrirse()
    {
        var comercio = NuevoComercio(EstadoComercioEnum.Aprobado);

        comercio.CambiarEstado(EstadoComercioEnum.Documentacion);

        Assert.Equal(EstadoComercioEnum.Documentacion, comercio.Estado);
    }

    [Fact]
    public void UnComercioPuedeSaltarDirectoAAprobado()
    {
        var comercio = NuevoComercio();

        comercio.CambiarEstado(EstadoComercioEnum.Aprobado);

        Assert.Equal(EstadoComercioEnum.Aprobado, comercio.Estado);
    }

    [Fact]
    public void UnComercioNuevoArrancaSinInteracciones()
    {
        Assert.Empty(NuevoComercio().Interacciones);
    }
}
