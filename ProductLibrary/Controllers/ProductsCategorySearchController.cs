using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductLibrary.Contracts;
using ProductLibrary.Dtos;

namespace ProductLibrary.Controllers;

/// <summary>
/// Controller generico OPZIONALE: qui vive il comportamento "diverso" che
/// prima era il metodo GetByCategory scritto a mano nel controller di
/// Consumer B. Ora, poiché niente controller può stare nei progetti consumer,
/// questo comportamento diventa un secondo controller generico nella libreria,
/// che solo i consumer con entità compatibili (TEntity : IHasCategory)
/// possono registrare.
///
/// Questo è il costo/beneficio della scelta "tutto in libreria": la
/// divergenza tra consumer non si esprime più con classi diverse nei
/// progetti consumer, ma con COMBINAZIONI diverse di controller generici
/// registrati (Consumer A registra solo il CRUD, Consumer B registra
/// CRUD + questo).
/// </summary>
[ApiController]
[Route("api/products")] // Rotta di default, sovrascrivibile per singolo consumer in AddCategorySearchController(...)
public class ProductsCategorySearchController<TContext, TEntity, TDto, TKey> : ControllerBase
    where TContext : DbContext
    where TEntity : class, IIdentifiable<TKey>, IHasDisplayName, IHasPrice, IHasCategory
    where TDto : ProductDto<TKey>
    where TKey : IEquatable<TKey>
{
    private readonly TContext _db;
    private readonly IMapper _mapper;

    public ProductsCategorySearchController(TContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    [HttpGet("by-category/{category}")]
    public virtual async Task<ActionResult<IEnumerable<TDto>>> GetByCategory(string category)
    {
        var result = await _db.Set<TEntity>()
            .Where(e => EF.Property<string>(e, "Category") == category)
            .ProjectTo<TDto>(_mapper.ConfigurationProvider)
            .ToListAsync();

        return Ok(result);
    }
}
