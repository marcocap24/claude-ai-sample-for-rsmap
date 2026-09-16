using ConsumerA.Api.Data;
using ConsumerA.Api.Dtos;
using ConsumerA.Api.Entities;
using ConsumerA.Api.Mapping;
using ConsumerA.Api.Services;
using Microsoft.EntityFrameworkCore;
using ProductLibrary.Contracts;
using ProductLibrary.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Nessuna classe Controller in questo progetto: il controller CRUD "vive"
// nella libreria come generico chiuso, qui viene solo dichiarato con i
// tipi concreti di Consumer A e la rotta desiderata.
var registry = new GenericControllerRegistry()
    .AddCrudController<AppDbContext, ProductEntity, ProductDtoA, int>(); // nessuna rotta esplicita: usa il default "api/products" dichiarato in libreria

builder.Services
    .AddControllers()
    .AddLibraryGenericControllers(registry);

// Registrato contro l'interfaccia CHIUSA IPostProjectionProcessor<ProductDtoA>:
// il PostProjectionProcessingFilter (globale, aggiunto da AddLibraryGenericControllers)
// chiude a runtime l'attributo ereditato da ProductDto<TKey> proprio su questa
// interfaccia e la risolve da DI. Se non registrassimo nulla qui, non
// succederebbe nulla: nessun errore, nessun post-processing per ProductDtoA.
builder.Services.AddScoped<IPostProjectionProcessor<ProductDtoA>, ProductPriceTierProcessor>();

// EF InMemory solo per rendere lo scheletro eseguibile senza un DB reale.
builder.Services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("DbA"));

builder.Services.AddAutoMapper(typeof(ProductProfileA).Assembly);

var app = builder.Build();

app.MapControllers();
app.Run();
