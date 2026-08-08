using ZocoTasks.Domain.Common;
using ZocoTasks.Domain.Enums;
using ZocoTasks.Domain.Exceptions;

namespace ZocoTasks.Tests.Unit;

/// <summary>
/// Reglas del pipeline comercial.
/// </summary>
/// <remarks>
/// Estos tests no necesitan base, ni contenedor de dependencias, ni mocks:
/// <c>MaquinaEstadoComercio</c> es una clase estatica pura. Esa es la
/// contrapartida concreta de haber mantenido Domain sin dependencias externas.
/// </remarks>
public class MaquinaEstadoComercioTests
{
    // ---------------------------------------------------------------
    // Transiciones validas
    // ---------------------------------------------------------------

    [Theory]
    [InlineData(EstadoComercioEnum.Nuevo, EstadoComercioEnum.Contactado)]
    [InlineData(EstadoComercioEnum.Contactado, EstadoComercioEnum.Interesado)]
    [InlineData(EstadoComercioEnum.Interesado, EstadoComercioEnum.Documentacion)]
    [InlineData(EstadoComercioEnum.Documentacion, EstadoComercioEnum.Aprobado)]
    public void PuedeAvanzarAlSiguienteEstadoDelEmbudo(
        EstadoComercioEnum desde, EstadoComercioEnum hacia)
    {
        Assert.True(MaquinaEstadoComercio.PuedeTransicionar(desde, hacia));
    }

    [Theory]
    [InlineData(EstadoComercioEnum.Nuevo)]
    [InlineData(EstadoComercioEnum.Contactado)]
    [InlineData(EstadoComercioEnum.Interesado)]
    [InlineData(EstadoComercioEnum.Documentacion)]
    public void PuedeRechazarseDesdeCualquierEstadoNoTerminal(EstadoComercioEnum desde)
    {
        // Un comercio puede caerse en cualquier punto del embudo, no solo al final.
        Assert.True(MaquinaEstadoComercio.PuedeTransicionar(desde, EstadoComercioEnum.Rechazado));
    }

    // ---------------------------------------------------------------
    // Transiciones invalidas
    // ---------------------------------------------------------------

    [Theory]
    [InlineData(EstadoComercioEnum.Nuevo, EstadoComercioEnum.Interesado)]
    [InlineData(EstadoComercioEnum.Nuevo, EstadoComercioEnum.Aprobado)]
    [InlineData(EstadoComercioEnum.Contactado, EstadoComercioEnum.Documentacion)]
    [InlineData(EstadoComercioEnum.Interesado, EstadoComercioEnum.Aprobado)]
    public void NoPuedeSaltearEtapasDelEmbudo(
        EstadoComercioEnum desde, EstadoComercioEnum hacia)
    {
        Assert.False(MaquinaEstadoComercio.PuedeTransicionar(desde, hacia));
    }

    [Theory]
    [InlineData(EstadoComercioEnum.Contactado, EstadoComercioEnum.Nuevo)]
    [InlineData(EstadoComercioEnum.Interesado, EstadoComercioEnum.Contactado)]
    [InlineData(EstadoComercioEnum.Documentacion, EstadoComercioEnum.Interesado)]
    public void NoPuedeRetroceder(EstadoComercioEnum desde, EstadoComercioEnum hacia)
    {
        // El pipeline de la consigna es lineal. Si el negocio pidiera permitir
        // retrocesos, el cambio es en el diccionario de MaquinaEstadoComercio
        // y este test es el que hay que actualizar.
        Assert.False(MaquinaEstadoComercio.PuedeTransicionar(desde, hacia));
    }

    [Theory]
    [InlineData(EstadoComercioEnum.Aprobado)]
    [InlineData(EstadoComercioEnum.Rechazado)]
    public void LosEstadosTerminalesNoTienenSalida(EstadoComercioEnum final)
    {
        Assert.True(MaquinaEstadoComercio.EsFinal(final));
        Assert.Empty(MaquinaEstadoComercio.TransicionesDesde(final));

        foreach (var destino in Enum.GetValues<EstadoComercioEnum>())
        {
            Assert.False(MaquinaEstadoComercio.PuedeTransicionar(final, destino));
        }
    }

    [Fact]
    public void NingunEstadoPuedeTransicionarASiMismo()
    {
        foreach (var estado in Enum.GetValues<EstadoComercioEnum>())
        {
            Assert.False(MaquinaEstadoComercio.PuedeTransicionar(estado, estado));
        }
    }

    // ---------------------------------------------------------------
    // Clasificacion de estados
    // ---------------------------------------------------------------

    [Theory]
    [InlineData(EstadoComercioEnum.Nuevo)]
    [InlineData(EstadoComercioEnum.Contactado)]
    [InlineData(EstadoComercioEnum.Interesado)]
    [InlineData(EstadoComercioEnum.Documentacion)]
    public void LosEstadosIntermediosNoSonTerminales(EstadoComercioEnum estado)
    {
        Assert.False(MaquinaEstadoComercio.EsFinal(estado));
    }

    [Fact]
    public void TodoEstadoNoTerminalTieneAlMenosUnaSalida()
    {
        var noTerminales = Enum.GetValues<EstadoComercioEnum>()
            .Where(e => !MaquinaEstadoComercio.EsFinal(e));

        foreach (var estado in noTerminales)
        {
            Assert.NotEmpty(MaquinaEstadoComercio.TransicionesDesde(estado));
        }
    }

    // ---------------------------------------------------------------
    // ValidarTransicion: la version que lanza
    // ---------------------------------------------------------------

    [Fact]
    public void ValidarTransicionNoLanzaSiLaTransicionEsValida()
    {
        var excepcion = Record.Exception(() =>
            MaquinaEstadoComercio.ValidarTransicion(
                EstadoComercioEnum.Nuevo, EstadoComercioEnum.Contactado));

        Assert.Null(excepcion);
    }

    [Fact]
    public void ValidarTransicionLanzaConLosDosEstadosEnLaExcepcion()
    {
        var ex = Assert.Throws<EstadoTransicionInvalidaException>(() =>
            MaquinaEstadoComercio.ValidarTransicion(
                EstadoComercioEnum.Nuevo, EstadoComercioEnum.Aprobado));

        // La excepcion lleva los dos estados para que la API pueda armar un
        // mensaje util en lugar de un error generico.
        Assert.Equal(EstadoComercioEnum.Nuevo, ex.Desde);
        Assert.Equal(EstadoComercioEnum.Aprobado, ex.Hacia);
        Assert.Equal("estado_transicion_invalida", ex.Codigo);
    }
}
