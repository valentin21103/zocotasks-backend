using FluentValidation;
using ZocoTasks.Business.DTOs;

namespace ZocoTasks.Business.Validators;

/// <summary>
/// Validacion de formato de los datos de un comercio.
/// </summary>
/// <remarks>
/// Aca solo se valida lo que puede decidirse mirando el DTO. Lo que exige
/// consultar la base —que el CUIT no este repetido, que el rubro exista— lo
/// resuelve <c>ComercioService</c> y lanza <c>ReglaDeNegocioException</c>.
/// La separacion importa: esto devuelve 400 con el detalle por campo, aquello
/// devuelve 422.
///
/// El alta y la edicion repiten reglas porque son DTOs distintos. Es
/// duplicacion deliberada: mantenerlos independientes permite que la edicion
/// diverja del alta sin romper nada.
/// </remarks>
public class CrearComercioDtoValidator : AbstractValidator<CrearComercioDto>
{
    public CrearComercioDtoValidator()
    {
        RuleFor(x => x.NombreComercial)
            .NotEmpty().WithMessage("El nombre comercial es obligatorio.")
            .MaximumLength(150).WithMessage("El nombre comercial no puede superar los 150 caracteres.");

        RuleFor(x => x.Cuit)
            .NotEmpty().WithMessage("El CUIT es obligatorio.")
            .Must(cuit => cuit.Count(char.IsDigit) == 11)
            .WithMessage("El CUIT debe tener 11 digitos.");

        RuleFor(x => x.NombreContacto)
            .NotEmpty().WithMessage("El nombre del contacto es obligatorio.")
            .MaximumLength(120).WithMessage("El nombre del contacto no puede superar los 120 caracteres.");

        // Permisivo a proposito: los telefonos argentinos se escriben de muchas
        // formas y rechazar una valida molesta mas de lo que aporta.
        RuleFor(x => x.Telefono)
            .MaximumLength(30).WithMessage("El telefono no puede superar los 30 caracteres.")
            .Matches(@"^[\d\s\-\(\)\+]+$")
            .WithMessage("El telefono solo puede contener numeros, espacios, guiones y parentesis.")
            .When(x => !string.IsNullOrWhiteSpace(x.Telefono));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("El email no tiene un formato valido.")
            .MaximumLength(150).WithMessage("El email no puede superar los 150 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.RubroId)
            .GreaterThan(0).WithMessage("Hay que seleccionar un rubro.");

        RuleFor(x => x.Notas)
            .MaximumLength(4000).WithMessage("Las notas no pueden superar los 4000 caracteres.");
    }
}

public class ActualizarComercioDtoValidator : AbstractValidator<ActualizarComercioDto>
{
    public ActualizarComercioDtoValidator()
    {
        RuleFor(x => x.NombreComercial)
            .NotEmpty().WithMessage("El nombre comercial es obligatorio.")
            .MaximumLength(150).WithMessage("El nombre comercial no puede superar los 150 caracteres.");

        RuleFor(x => x.Cuit)
            .NotEmpty().WithMessage("El CUIT es obligatorio.")
            .Must(cuit => cuit.Count(char.IsDigit) == 11)
            .WithMessage("El CUIT debe tener 11 digitos.");

        RuleFor(x => x.NombreContacto)
            .NotEmpty().WithMessage("El nombre del contacto es obligatorio.")
            .MaximumLength(120).WithMessage("El nombre del contacto no puede superar los 120 caracteres.");

        RuleFor(x => x.Telefono)
            .MaximumLength(30).WithMessage("El telefono no puede superar los 30 caracteres.")
            .Matches(@"^[\d\s\-\(\)\+]+$")
            .WithMessage("El telefono solo puede contener numeros, espacios, guiones y parentesis.")
            .When(x => !string.IsNullOrWhiteSpace(x.Telefono));

        RuleFor(x => x.Email)
            .EmailAddress().WithMessage("El email no tiene un formato valido.")
            .MaximumLength(150).WithMessage("El email no puede superar los 150 caracteres.")
            .When(x => !string.IsNullOrWhiteSpace(x.Email));

        RuleFor(x => x.RubroId)
            .GreaterThan(0).WithMessage("Hay que seleccionar un rubro.");

        RuleFor(x => x.Notas)
            .MaximumLength(4000).WithMessage("Las notas no pueden superar los 4000 caracteres.");
    }
}

public class CambiarEstadoDtoValidator : AbstractValidator<CambiarEstadoDto>
{
    public CambiarEstadoDtoValidator()
    {
        // Solo valida que el valor exista en el enum. Que no sea el mismo
        // estado que el comercio ya tiene lo valida Comercio.CambiarEstado.
        RuleFor(x => x.NuevoEstado)
            .IsInEnum().WithMessage("El estado indicado no existe.");
    }
}

public class CrearInteraccionDtoValidator : AbstractValidator<CrearInteraccionDto>
{
    public CrearInteraccionDtoValidator()
    {
        RuleFor(x => x.Tipo)
            .IsInEnum().WithMessage("El tipo de interaccion indicado no existe.");

        RuleFor(x => x.Detalle)
            .NotEmpty().WithMessage("El detalle de la interaccion es obligatorio.")
            .MaximumLength(2000).WithMessage("El detalle no puede superar los 2000 caracteres.");

        // Se admite cargar interacciones pasadas, pero no futuras: registrar una
        // llamada que todavia no ocurrio no tiene sentido en un seguimiento.
        // El margen de un dia absorbe diferencias de huso horario del cliente.
        RuleFor(x => x.Fecha)
            .LessThanOrEqualTo(_ => DateTime.UtcNow.AddDays(1))
            .WithMessage("La fecha de la interaccion no puede ser futura.")
            .When(x => x.Fecha.HasValue);
    }
}
