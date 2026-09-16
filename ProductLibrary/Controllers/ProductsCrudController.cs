using AutoMapper;
using AutoMapper.QueryableExtensions;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using ProductLibrary.Contracts;
using ProductLibrary.Dtos;

namespace ProductLibrary.Controllers;

/// <summary>
/// Controller CRUD generico. Il TIPO APERTO vive qui, nella libreria: nessun
/// consumer scrive una classe controller. Ogni consumer, in Program.cs, lo
/// registra come generico CHIUSO (via GenericControllerRegistry) specificando
/// i propri tipi concreti: TContext (il proprio DbContext), TEntity (la propria
/// entità), TDto (il proprio DTO esteso), TKey (il proprio tipo di chiave).
///
/// Il tipo generico aperto NON viene mai scoperto dal discovery automatico di
/// ASP.NET Core (i tipi generici aperti sono esclusi di default): la sua
/// registrazione come controller "reale" avviene tramite
/// GenericControllerFeatureProvider + GenericControllerRouteConvention
/// (vedi Infrastructure/).
///
/// NB: qui non c'è alcuna chiamata esplicita a un "post-projection processor":
/// se TDto è decorato con [PostProjectionProcessor], viene applicato in modo
/// completamente automatico dal PostProjectionProcessingFilter, registrato
/// globalmente da AddLibraryGenericControllers (vedi Infrastructure/ e
/// Filters/). Il controller non sa nemmeno che quel meccanismo esiste.
/// </summary>
[ApiController]
[Route("api/products")] // Rotta di default: usata quando il consumer non ne specifica una propria in AddCrudController(...)
public class ProductsCrudController<TContext, TEntity, TDto, TKey> : ControllerBase
    where TContext : DbContext
    where TEntity : class, IIdentifiable<TKey>, IHasDisplayName, IHasPrice, new()
    where TDto : ProductDto<TKey>, new()
    where TKey : IEquatable<TKey>, IParsable<TKey>
{
    private readonly TContext _db;
    private readonly IMapper _mapper;

    public ProductsCrudController(TContext db, IMapper mapper)
    {
        _db = db;
        _mapper = mapper;
    }

    [HttpGet]
    public virtual async Task<ActionResult<IEnumerable<TDto>>> GetAll(CancellationToken cancellationToken)
    {
        var result = await _db.Set<TEntity>()
            .ProjectTo<TDto>(_mapper.ConfigurationProvider)
            .ToListAsync(cancellationToken);

        return Ok(result);
    }

    [HttpGet("{id}")]
    public virtual async Task<ActionResult<TDto>> GetById(TKey id, CancellationToken cancellationToken)
    {
        // NB: usiamo EF.Property<TKey>(e, "Id") invece di "e.Id" (che passerebbe
        // per la proprietà dell'INTERFACCIA IIdentifiable<TKey>): EF Core non
        // riesce a tradurre in SQL un accesso a una proprietà di interfaccia in
        // un contesto generico, mentre EF.Property risolve per nome mappato
        // sull'entità concreta ed è sempre traducibile.
        var dto = await _db.Set<TEntity>()
            .Where(e => EF.Property<TKey>(e, "Id")!.Equals(id))
            .ProjectTo<TDto>(_mapper.ConfigurationProvider)
            .FirstOrDefaultAsync(cancellationToken);

        return dto is null ? NotFound() : Ok(dto);
    }

    [HttpPost]
    public virtual async Task<ActionResult<TDto>> Create([FromBody] TDto input, CancellationToken cancellationToken)
    {
        var entity = _mapper.Map<TEntity>(input);

        _db.Set<TEntity>().Add(entity);
        await _db.SaveChangesAsync(cancellationToken);

        var dto = _mapper.Map<TDto>(entity);
        return CreatedAtAction(nameof(GetById), new { id = dto.Id }, dto);
    }

    [HttpPut("{id}")]
    public virtual async Task<IActionResult> Update(TKey id, [FromBody] TDto input, CancellationToken cancellationToken)
    {
        var entity = await _db.Set<TEntity>().FindAsync([id], cancellationToken);
        if (entity is null) return NotFound();

        _mapper.Map(input, entity); // map "in place" sull'entità esistente
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpDelete("{id}")]
    public virtual async Task<IActionResult> Delete(TKey id, CancellationToken cancellationToken)
    {
        var entity = await _db.Set<TEntity>().FindAsync([id], cancellationToken);
        if (entity is null) return NotFound();

        _db.Set<TEntity>().Remove(entity);
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }
}
