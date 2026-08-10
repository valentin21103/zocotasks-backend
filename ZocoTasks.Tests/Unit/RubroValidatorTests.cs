using ZocoTasks.Business.DTOs;
using ZocoTasks.Business.Validators;

namespace ZocoTasks.Tests.Unit;

/// <summary>
/// Validacion de formato del rubro.
/// </summary>
/// <remarks>
/// Que el nombre este repetido no se prueba aca porque no se valida aca: eso
/// exige consultar la base y vive en <c>RubroService</c>, que lanza
/// <c>ReglaDeNegocioException</c> y termina en un 422. Este validador solo
/// decide lo que puede decidirse mirando el DTO.
/// </remarks>
public class RubroValidatorTests
{
    private readonly GuardarRubroDtoValidator _validador = new();

    [Fact]
    public void UnNombreNormalEsValido()
    {
        var resultado = _validador.Validate(new GuardarRubroDto { Nombre = "Farmacia" });

        Assert.True(resultado.IsValid);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public void ElNombreEsObligatorio(string nombre)
    {
        var resultado = _validador.Validate(new GuardarRubroDto { Nombre = nombre });

        Assert.False(resultado.IsValid);
        Assert.Contains(resultado.Errors, e => e.PropertyName == nameof(GuardarRubroDto.Nombre));
    }

    [Fact]
    public void ElNombreNoPuedeSuperarLosCienCaracteres()
    {
        var resultado = _validador.Validate(
            new GuardarRubroDto { Nombre = new string('a', 101) });

        Assert.False(resultado.IsValid);
    }

    [Fact]
    public void CienCaracteresJustosEntran()
    {
        // El borde exacto: 100 es el maximo que declara la columna.
        var resultado = _validador.Validate(
            new GuardarRubroDto { Nombre = new string('a', 100) });

        Assert.True(resultado.IsValid);
    }
}
