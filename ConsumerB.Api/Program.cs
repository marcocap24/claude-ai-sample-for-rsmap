using ConsumerB.Api.Data;
using ConsumerB.Api.Dtos;
using ConsumerB.Api.Entities;
using ConsumerB.Api.Mapping;
using Microsoft.EntityFrameworkCore;
using ProductLibrary.Infrastructure;

var builder = WebApplication.CreateBuilder(args);

// Consumer B registra il CRUD (come A) più il controller opzionale di ricerca
// per categoria: la "divergenza" tra i due consumer ora si esprime scegliendo
// quali controller generici della libreria attivare, non scrivendo classi.
var registry = new GenericControllerRegistry()
    .AddCrudController<AppDbContext, ProductEntity, ProductDtoB, string>() // default "api/products"
    .AddCategorySearchController<AppDbContext, ProductEntity, ProductDtoB, string>(); // idem; passa una stringa qui solo se ti serve una rotta diversa

builder.Services
    .AddControllers()
    .AddLibraryGenericControllers(registry);

builder.Services.AddDbContext<AppDbContext>(opt => opt.UseInMemoryDatabase("DbB"));

builder.Services.AddAutoMapper(typeof(ProductProfileB).Assembly);

var app = builder.Build();

app.MapControllers();
app.Run();
