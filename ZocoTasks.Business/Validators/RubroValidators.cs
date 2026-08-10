using FluentValidation;
using ZocoTasks.Business.DTOs;

namespace ZocoTasks.Business.Validators;

/// <summary>
/// Validacion de formato del rubro.
/// </summary>
/// <remarks>
/// Que el nombre no este repetido no se valida aca: eso exige consultar la
/// base y lo resuelve <c>RubroService</c> lanzando <c>ReglaDeNegocioException</c>.
/// Uno devuelve 400 con el detalle por campo, el otro 422.
/// </remarks>
public class GuardarRubroDtoValidator : AbstractValidator<GuardarRubroDto>
{
    public GuardarRubroDtoValidator()
    {
        RuleFor(x => x.Nombre)
            .NotEmpty().WithMessage("El nombre del rubro es obligatorio.")
            .MaximumLength(100).WithMessage("El nombre del rubro no puede superar los 100 caracteres.");
    }
}
