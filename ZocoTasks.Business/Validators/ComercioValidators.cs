using FluentValidation;
using ZocoTasks.Business.DTOs;
using ZocoTasks.Domain.Common;

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

        // La validacion del digito verificador vive en Domain: es una regla del
        // negocio, no del formulario.
        RuleFor(x => x.Cuit)
            .NotEmpty().WithMessage("El CUIT es obligatorio.")
            .Must(Cuit.EsValido)
            .WithMessage("El CUIT no es valido: no pasa la verificacion por modulo 11.");

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
            .Must(Cuit.EsValido)
            .WithMessage("El CUIT no es valido: no pasa la verificacion por modulo 11.");

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
        // Solo valida que el valor exista en el enum. Si la transicion es
        // posible desde el estado actual lo decide la maquina de estados del
        // dominio, que es la unica que conoce el estado en que esta el comercio.
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
