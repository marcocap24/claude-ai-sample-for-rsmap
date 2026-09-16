using Microsoft.EntityFrameworkCore;
using ProductLibrary.Contracts;
using ProductLibrary.Controllers;
using ProductLibrary.Dtos;

namespace ProductLibrary.Infrastructure;

/// <summary>
/// Un singolo controller generico chiuso da esporre, con l'eventuale template
/// di rotta da assegnargli. Se RouteTemplate è null, la convention non tocca
/// il controller: resta valido il [Route] statico già dichiarato sulla classe
/// generica aperta in libreria (vedi ProductsCrudController/ProductsCategorySearchController).
/// </summary>
public sealed record GenericControllerRegistration(Type ControllerType, string? RouteTemplate);

/// <summary>
/// Punto di configurazione che ogni consumer usa in Program.cs per dichiarare
/// quali controller generici della libreria vuole esporre, con quali tipi
/// concreti e (facoltativamente) su quale rotta. Sostituisce completamente la
/// scrittura manuale di classi Controller nei progetti consumer.
/// </summary>
public sealed class GenericControllerRegistry
{
    private readonly List<GenericControllerRegistration> _registrations = new();

    public IReadOnlyList<GenericControllerRegistration> Registrations => _registrations;

    /// <param name="routeTemplate">
    /// Facoltativo. Se non valorizzato, resta valida la rotta di default
    /// dichiarata staticamente su <see cref="ProductsCrudController{TContext,TEntity,TDto,TKey}"/>
    /// (attualmente "api/products"). Valorizzalo solo se questo consumer ha
    /// bisogno di una rotta diversa da quella di default.
    /// </param>
    public GenericControllerRegistry AddCrudController<TContext, TEntity, TDto, TKey>(string? routeTemplate = null)
        where TContext : DbContext
        where TEntity : class, IIdentifiable<TKey>, IHasDisplayName, IHasPrice, new()
        where TDto : ProductDto<TKey>, new()
        where TKey : IEquatable<TKey>, IParsable<TKey>
    {
        var closedType = typeof(ProductsCrudController<,,,>)
            .MakeGenericType(typeof(TContext), typeof(TEntity), typeof(TDto), typeof(TKey));

        _registrations.Add(new GenericControllerRegistration(closedType, routeTemplate));
        return this;
    }

    /// <param name="routeTemplate">
    /// Facoltativo, stessa logica di <see cref="AddCrudController{TContext,TEntity,TDto,TKey}"/>:
    /// se omesso resta valida la rotta di default dichiarata su
    /// <see cref="ProductsCategorySearchController{TContext,TEntity,TDto,TKey}"/>.
    /// </param>
    public GenericControllerRegistry AddCategorySearchController<TContext, TEntity, TDto, TKey>(string? routeTemplate = null)
        where TContext : DbContext
        where TEntity : class, IIdentifiable<TKey>, IHasDisplayName, IHasPrice, IHasCategory
        where TDto : ProductDto<TKey>
        where TKey : IEquatable<TKey>
    {
        var closedType = typeof(ProductsCategorySearchController<,,,>)
            .MakeGenericType(typeof(TContext), typeof(TEntity), typeof(TDto), typeof(TKey));

        _registrations.Add(new GenericControllerRegistration(closedType, routeTemplate));
        return this;
    }
}
