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
///
/// El movimiento entre estados es libre por decision de negocio: lo unico que
/// la maquina impide es transicionar de un estado a si mismo.
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

    [Theory]
    [InlineData(EstadoComercioEnum.Nuevo, EstadoComercioEnum.Aprobado)]
    [InlineData(EstadoComercioEnum.Nuevo, EstadoComercioEnum.Documentacion)]
    [InlineData(EstadoComercioEnum.Contactado, EstadoComercioEnum.Aprobado)]
    public void PuedeSaltearEtapasDelEmbudo(EstadoComercioEnum desde, EstadoComercioEnum hacia)
    {
        // El embudo tiene un orden natural, pero no obliga a recorrerlo paso a
        // paso: un comercio puede llegar ya decidido.
        Assert.True(MaquinaEstadoComercio.PuedeTransicionar(desde, hacia));
    }

    [Theory]
    [InlineData(EstadoComercioEnum.Contactado, EstadoComercioEnum.Nuevo)]
    [InlineData(EstadoComercioEnum.Interesado, EstadoComercioEnum.Contactado)]
    [InlineData(EstadoComercioEnum.Documentacion, EstadoComercioEnum.Interesado)]
    public void PuedeRetrocederParaCorregirUnaCargaMal(
        EstadoComercioEnum desde, EstadoComercioEnum hacia)
    {
        // Sin retroceso, un estado cargado por error dejaba al comercio trabado
        // sin ninguna forma de arreglarlo.
        Assert.True(MaquinaEstadoComercio.PuedeTransicionar(desde, hacia));
    }

    [Theory]
    [InlineData(EstadoComercioEnum.Aprobado, EstadoComercioEnum.Documentacion)]
    [InlineData(EstadoComercioEnum.Rechazado, EstadoComercioEnum.Contactado)]
    public void UnaOportunidadCerradaPuedeReabrirse(
        EstadoComercioEnum desde, EstadoComercioEnum hacia)
    {
        Assert.True(MaquinaEstadoComercio.PuedeTransicionar(desde, hacia));
    }

    // ---------------------------------------------------------------
    // La unica transicion invalida
    // ---------------------------------------------------------------

    [Fact]
    public void NingunEstadoPuedeTransicionarASiMismo()
    {
        // Es la unica regla que queda: no es un cambio de estado.
        foreach (var estado in Enum.GetValues<EstadoComercioEnum>())
        {
            Assert.False(MaquinaEstadoComercio.PuedeTransicionar(estado, estado));
        }
    }

    [Fact]
    public void UnEstadoInexistenteNoEsUnDestinoValido()
    {
        // Protege el borde: un smallint arbitrario casteado al enum no pasa.
        Assert.False(MaquinaEstadoComercio.PuedeTransicionar(
            EstadoComercioEnum.Nuevo, (EstadoComercioEnum)99));
    }

    // ---------------------------------------------------------------
    // Transiciones disponibles
    // ---------------------------------------------------------------

    [Fact]
    public void TodoEstadoOfreceComoDestinoATodosLosDemas()
    {
        var todos = Enum.GetValues<EstadoComercioEnum>();

        foreach (var estado in todos)
        {
            var destinos = MaquinaEstadoComercio.TransicionesDesde(estado);

            Assert.Equal(todos.Length - 1, destinos.Count);
            Assert.DoesNotContain(estado, destinos);
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

    [Theory]
    [InlineData(EstadoComercioEnum.Aprobado)]
    [InlineData(EstadoComercioEnum.Rechazado)]
    public void AprobadoYRechazadoSiguenSiendoLosEstadosQueCierran(EstadoComercioEnum estado)
    {
        // EsFinal quedo como clasificacion para reportes y para la columna
        // es_final del catalogo: ya no bloquea la salida, porque una
        // oportunidad cerrada puede reabrirse.
        Assert.True(MaquinaEstadoComercio.EsFinal(estado));
        Assert.NotEmpty(MaquinaEstadoComercio.TransicionesDesde(estado));
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
                EstadoComercioEnum.Nuevo, EstadoComercioEnum.Nuevo));

        // La excepcion lleva los dos estados para que la API pueda armar un
        // mensaje util en lugar de un error generico.
        Assert.Equal(EstadoComercioEnum.Nuevo, ex.Desde);
        Assert.Equal(EstadoComercioEnum.Nuevo, ex.Hacia);
        Assert.Equal("estado_transicion_invalida", ex.Codigo);
    }
}
