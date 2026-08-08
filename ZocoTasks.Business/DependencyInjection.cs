using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using ZocoTasks.Business.Interfaces;
using ZocoTasks.Business.Services;

namespace ZocoTasks.Business;

/// <summary>
/// Registro de la capa de aplicacion: servicios y validadores.
/// </summary>
public static class DependencyInjection
{
    public static IServiceCollection AddBusiness(this IServiceCollection services)
    {
        services.AddScoped<IComercioService, ComercioService>();
        services.AddScoped<IInteraccionService, InteraccionService>();
        services.AddScoped<ICatalogoService, CatalogoService>();

        // Registra por reflexion todos los AbstractValidator de este ensamblado.
        // Asi agregar un validador nuevo no obliga a acordarse de registrarlo.
        services.AddValidatorsFromAssembly(typeof(DependencyInjection).Assembly);

        return services;
    }
}
