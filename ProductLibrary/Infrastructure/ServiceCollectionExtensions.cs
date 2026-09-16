using Microsoft.Extensions.DependencyInjection;
using ProductLibrary.Filters;

namespace ProductLibrary.Infrastructure;

/// <summary>
/// Punto di ingresso unico per il consumer: una sola chiamata in Program.cs
/// che collega il registry ai meccanismi interni di MVC (feature provider +
/// convention di routing) e registra globalmente il filtro che applica i
/// [PostProjectionProcessor]. Il consumer non deve conoscere questi dettagli,
/// né richiamare nulla esplicitamente nei propri controller/action: il
/// post-processing è automatico per qualunque azione, generica o scritta a
/// mano, che restituisca un DTO decorato con l'attributo.
/// </summary>
public static class GenericControllersServiceCollectionExtensions
{
    public static IMvcBuilder AddLibraryGenericControllers(this IMvcBuilder builder, GenericControllerRegistry registry)
    {
        builder.Services.AddSingleton(registry);

        builder.ConfigureApplicationPartManager(apm =>
            apm.FeatureProviders.Add(new GenericControllerFeatureProvider(registry)));

        builder.AddMvcOptions(options =>
        {
            options.Conventions.Add(new GenericControllerRouteConvention(registry));
            options.Filters.Add<PostProjectionProcessingFilter>();
        });

        return builder;
    }
}

